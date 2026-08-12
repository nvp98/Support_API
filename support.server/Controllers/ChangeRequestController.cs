using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;
using support.server.Models.SLC;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChangeRequestController : ControllerBase
{
    private readonly AppDbContext _context;
    public ChangeRequestController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? projectId,
        [FromQuery] int? moduleId,
        [FromQuery] byte? status,
        [FromQuery] byte? priority,
        [FromQuery] string? requestorCode,
        [FromQuery] string? keyword,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.ChangeRequests
            .Include(cr => cr.Module)
            .Include(cr => cr.Project)
            .AsQueryable();

        if (projectId.HasValue) query = query.Where(cr => cr.ProjectId == projectId.Value);
        if (moduleId.HasValue) query = query.Where(cr => cr.ModuleId == moduleId.Value);
        if (status.HasValue) query = query.Where(cr => cr.Status == status.Value);
        if (priority.HasValue) query = query.Where(cr => cr.Priority == priority.Value);
        if (!string.IsNullOrEmpty(requestorCode))
            query = query.Where(cr => cr.RequestorCode == requestorCode);
        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(cr => cr.Title.Contains(keyword) || cr.Code.Contains(keyword));
        if (fromDate.HasValue) query = query.Where(cr => cr.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(cr => cr.CreatedAt <= toDate.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(cr => cr.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(cr => new
            {
                cr.Id, cr.Code, cr.Title, cr.Priority, cr.Status,
                cr.ModuleId, ModuleName = cr.Module != null ? cr.Module.Name : null,
                cr.ProjectId, ProjectName = cr.Project != null ? cr.Project.Name : null,
                cr.RequestorCode, cr.RequestorName, cr.RequestorDept,
                cr.ApproverCode, cr.ApproverName,
                cr.DeveloperCode, cr.DeveloperName,
                cr.ImpactTimeline, cr.ImpactDays, cr.ImpactVersion,
                cr.CurrentRevision, cr.CreatedAt, cr.ApprovedAt, cr.CompletedAt,
                RevisionCount = cr.Revisions.Count()
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.ChangeRequests
            .Include(cr => cr.Module)
            .Include(cr => cr.Project)
            .Include(cr => cr.Revisions.OrderBy(r => r.RevisionNumber))
            .FirstOrDefaultAsync(cr => cr.Id == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChangeRequest dto)
    {
        // Auto-generate code: REQ-YYMMDD-XXXX
        var today = DateTime.Now;
        var todayStr = today.ToString("yyMMdd");
        var count = await _context.ChangeRequests
            .CountAsync(cr => cr.CreatedAt.Date == today.Date) + 1;
        dto.Code = $"REQ-{todayStr}-{count:D4}";
        dto.Id = 0;
        dto.CreatedAt = today;
        dto.Status = 0; // Draft
        dto.CurrentRevision = 0;

        _context.ChangeRequests.Add(dto);
        await _context.SaveChangesAsync();

        // Create Rev0 automatically
        _context.ChangeRevisions.Add(new ChangeRevision
        {
            ChangeRequestId = dto.Id,
            RevisionNumber = 0,
            Content = dto.Content,
            Reason = dto.Reason,
            RequestorCode = dto.RequestorCode,
            RequestorName = dto.RequestorName,
            ImpactTimeline = dto.ImpactTimeline,
            ImpactDays = dto.ImpactDays,
            ImpactVersion = dto.ImpactVersion,
            Status = 0,
            CreatedAt = today
        });
        await _context.SaveChangesAsync();

        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ChangeRequest dto)
    {
        var item = await _context.ChangeRequests.FindAsync(id);
        if (item == null) return NotFound();
        item.Title = dto.Title;
        item.Content = dto.Content;
        item.Reason = dto.Reason;
        item.Priority = dto.Priority;
        item.ModuleId = dto.ModuleId;
        item.ProjectId = dto.ProjectId;
        item.DeveloperCode = dto.DeveloperCode;
        item.DeveloperName = dto.DeveloperName;
        item.ImpactTimeline = dto.ImpactTimeline;
        item.ImpactDays = dto.ImpactDays;
        item.ImpactVersion = dto.ImpactVersion;
        item.ImpactModules = dto.ImpactModules;
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>Advance workflow status</summary>
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] ChangeStatusDto dto)
    {
        var item = await _context.ChangeRequests
            .Include(cr => cr.Module).ThenInclude(m => m != null ? m.Project : null)
            .FirstOrDefaultAsync(cr => cr.Id == id);
        if (item == null) return NotFound();

        var previousStatus = item.Status;
        item.Status = dto.Status;

        if (dto.Status == 2) // Approved
        {
            item.ApproverCode = dto.ActorCode;
            item.ApproverName = dto.ActorName;
            item.ApprovedAt = DateTime.Now;
        }
        else if (dto.Status == 9) // Completed
        {
            item.CompletedAt = DateTime.Now;

            // If change impacts timeline, auto-adjust module/project dates
            if (item.ImpactTimeline && item.ImpactDays != 0 && item.ModuleId.HasValue)
            {
                var module = await _context.SlcModules.FindAsync(item.ModuleId.Value);
                if (module != null && module.EndDate.HasValue)
                {
                    var oldEnd = module.EndDate.Value;
                    var newEnd = oldEnd.AddDays(item.ImpactDays);

                    _context.TimelineAdjustments.Add(new TimelineAdjustment
                    {
                        EntityType = "Module",
                        EntityId = module.Id,
                        PreviousEndDate = oldEnd,
                        NewEndDate = newEnd,
                        AdjustmentDays = item.ImpactDays,
                        Reason = $"Change Request {item.Code} hoàn thành",
                        ChangeRequestId = item.Id,
                        ChangedBy = dto.ActorCode,
                        ChangedByName = dto.ActorName,
                        ChangedAt = DateTime.Now
                    });

                    module.EndDate = newEnd;
                }
            }
        }
        else if (dto.Status == 10) // Rejected
        {
            item.RejectedAt = DateTime.Now;
            item.RejectedReason = dto.Reason;
        }

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    /// <summary>Add a new revision to an existing change request</summary>
    [HttpPost("{id}/revisions")]
    public async Task<IActionResult> AddRevision(int id, [FromBody] ChangeRevision dto)
    {
        var cr = await _context.ChangeRequests
            .Include(cr => cr.Revisions)
            .FirstOrDefaultAsync(cr => cr.Id == id);
        if (cr == null) return NotFound();

        var nextRevision = cr.Revisions.Any()
            ? cr.Revisions.Max(r => r.RevisionNumber) + 1
            : 1;

        dto.Id = 0;
        dto.ChangeRequestId = id;
        dto.RevisionNumber = nextRevision;
        dto.CreatedAt = DateTime.Now;
        dto.Status = 0;

        _context.ChangeRevisions.Add(dto);

        cr.CurrentRevision = nextRevision;
        cr.Content = dto.Content;

        await _context.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.ChangeRequests
            .Include(cr => cr.Revisions)
            .FirstOrDefaultAsync(cr => cr.Id == id);
        if (item == null) return NotFound();
        _context.ChangeRevisions.RemoveRange(item.Revisions);
        _context.ChangeRequests.Remove(item);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class ChangeStatusDto
{
    public byte Status { get; set; }
    public string? ActorCode { get; set; }
    public string? ActorName { get; set; }
    public string? Reason { get; set; }
}
