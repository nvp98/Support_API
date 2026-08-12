using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SlcDashboardController : ControllerBase
{
    private readonly AppDbContext _context;
    public SlcDashboardController(AppDbContext context) => _context = context;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var softwareCount = await _context.SoftwareCatalogs.CountAsync(s => s.Status == 1);
        var projectCount = await _context.SlcProjects.CountAsync();
        var moduleCount = await _context.SlcModules.CountAsync();
        var taskCount = await _context.SlcTasks.CountAsync();
        var changeCount = await _context.ChangeRequests.CountAsync();
        var pendingChanges = await _context.ChangeRequests.CountAsync(cr => cr.Status < 9 && cr.Status != 10);

        // Delayed projects: EndDate passed but status != Completed
        var delayedProjects = await _context.SlcProjects
            .Where(p => p.EndDate < today && p.Status != 2 && p.Status != 4)
            .CountAsync();

        // Delayed modules
        var delayedModules = await _context.SlcModules
            .Where(m => m.EndDate < today && m.Status != 2)
            .CountAsync();

        // Project progress
        var projects = await _context.SlcProjects
            .Include(p => p.Software)
            .Include(p => p.Modules)
            .ToListAsync();

        var projectProgress = projects.Select(p =>
        {
            var modules = p.Modules.ToList();
            var totalWeight = modules.Sum(m => (double)m.Weight);
            var progress = totalWeight > 0
                ? modules.Sum(m => (double)(m.ActualProgress * m.Weight)) / totalWeight
                : 0;
            return new
            {
                p.Id, p.Code, p.Name,
                SoftwareName = p.Software?.Name,
                p.Status, p.StartDate, p.EndDate, p.GoLiveDate,
                Progress = Math.Round(progress, 2),
                ModuleCount = modules.Count,
                IsDelayed = p.EndDate < today && p.Status != 2 && p.Status != 4
            };
        }).ToList();

        // Developer workload
        var developerStats = await _context.SlcModules
            .Where(m => !string.IsNullOrEmpty(m.AssigneeCode) && m.Status != 2)
            .GroupBy(m => new { m.AssigneeCode, m.AssigneeName })
            .Select(g => new
            {
                AssigneeCode = g.Key.AssigneeCode,
                AssigneeName = g.Key.AssigneeName,
                ModuleCount = g.Count(),
                AvgProgress = g.Average(m => (double)m.ActualProgress)
            })
            .ToListAsync();

        // Modules with most changes
        var topModulesByChange = await _context.ChangeRequests
            .Where(cr => cr.ModuleId.HasValue)
            .GroupBy(cr => new { cr.ModuleId, ModuleName = cr.Module != null ? cr.Module.Name : null })
            .Select(g => new { g.Key.ModuleId, g.Key.ModuleName, ChangeCount = g.Count() })
            .OrderByDescending(x => x.ChangeCount)
            .Take(10)
            .ToListAsync();

        // Change requests by status
        var changeByStatus = await _context.ChangeRequests
            .GroupBy(cr => cr.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        // Recent timeline adjustments
        var recentAdjustments = await _context.TimelineAdjustments
            .OrderByDescending(ta => ta.ChangedAt)
            .Take(10)
            .ToListAsync();

        return Ok(new
        {
            softwareCount,
            projectCount,
            moduleCount,
            taskCount,
            changeCount,
            pendingChanges,
            delayedProjects,
            delayedModules,
            projectProgress,
            developerStats,
            topModulesByChange,
            changeByStatus,
            recentAdjustments
        });
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> GetTimeline(
        [FromQuery] int? projectId,
        [FromQuery] string? view = "module") // module | project | developer
    {
        if (view == "project")
        {
            var projects = await _context.SlcProjects
                .Include(p => p.Software)
                .Where(p => !projectId.HasValue || p.Id == projectId)
                .Select(p => new
                {
                    p.Id, p.Code, p.Name,
                    SoftwareName = p.Software != null ? p.Software.Name : null,
                    p.StartDate, p.EndDate, p.GoLiveDate, p.Status,
                    Progress = p.Modules.Any()
                        ? p.Modules.Sum(m => (double)(m.ActualProgress * m.Weight)) / p.Modules.Sum(m => (double)m.Weight)
                        : 0
                })
                .ToListAsync();
            return Ok(projects);
        }

        // Default: module view (Gantt data)
        var modules = await _context.SlcModules
            .Include(m => m.Project).ThenInclude(p => p!.Software)
            .Where(m => !projectId.HasValue || m.ProjectId == projectId)
            .Select(m => new
            {
                m.Id, m.Code, m.Name,
                m.ProjectId, ProjectName = m.Project != null ? m.Project.Name : null,
                SoftwareName = m.Project != null && m.Project.Software != null ? m.Project.Software.Name : null,
                m.AssigneeCode, m.AssigneeName,
                m.StartDate, m.EndDate, m.Status,
                m.ActualProgress, m.PlannedProgress, m.Weight
            })
            .ToListAsync();
        return Ok(modules);
    }

    [HttpGet("kpi")]
    public async Task<IActionResult> GetKpi(
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var query = _context.ChangeRequests.AsQueryable();

        if (month.HasValue)
            query = query.Where(cr => cr.CreatedAt.Year == targetYear && cr.CreatedAt.Month == month.Value);
        else
            query = query.Where(cr => cr.CreatedAt.Year == targetYear);

        var totalChanges = await query.CountAsync();
        var completedChanges = await query.CountAsync(cr => cr.Status == 9);
        var rejectedChanges = await query.CountAsync(cr => cr.Status == 10);
        var avgImpactDays = totalChanges > 0
            ? await query.Where(cr => cr.ImpactTimeline).AverageAsync(cr => (double?)cr.ImpactDays) ?? 0
            : 0;

        var completionRate = totalChanges > 0
            ? Math.Round((double)completedChanges / totalChanges * 100, 2)
            : 0;

        // Monthly trend
        var monthlyTrend = await _context.ChangeRequests
            .Where(cr => cr.CreatedAt.Year == targetYear)
            .GroupBy(cr => cr.CreatedAt.Month)
            .Select(g => new { Month = g.Key, Count = g.Count(), Completed = g.Count(x => x.Status == 9) })
            .OrderBy(x => x.Month)
            .ToListAsync();

        return Ok(new
        {
            totalChanges,
            completedChanges,
            rejectedChanges,
            completionRate,
            avgImpactDays = Math.Round(avgImpactDays, 1),
            monthlyTrend
        });
    }
}
