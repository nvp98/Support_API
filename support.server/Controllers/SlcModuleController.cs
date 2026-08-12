using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;
using support.server.Models.SLC;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SlcModuleController : ControllerBase
{
    private readonly AppDbContext _context;
    public SlcModuleController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? projectId,
        [FromQuery] byte? status,
        [FromQuery] string? assigneeCode,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.SlcModules
            .Include(m => m.Project).ThenInclude(p => p!.Software)
            .Include(m => m.ModuleProcessSteps).ThenInclude(mps => mps.ProcessStep)
            .AsQueryable();

        if (projectId.HasValue) query = query.Where(m => m.ProjectId == projectId.Value);
        if (status.HasValue) query = query.Where(m => m.Status == status.Value);
        if (!string.IsNullOrEmpty(assigneeCode))
            query = query.Where(m => m.AssigneeCode == assigneeCode);
        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(m => m.Name.Contains(keyword) || m.Code.Contains(keyword));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(m => new
            {
                m.Id, m.Code, m.Name, m.ProjectId,
                ProjectName = m.Project != null ? m.Project.Name : null,
                SoftwareName = m.Project != null && m.Project.Software != null ? m.Project.Software.Name : null,
                m.Description, m.AssigneeCode, m.AssigneeName,
                m.StartDate, m.EndDate, m.PlannedProgress, m.ActualProgress,
                m.Weight, m.Status, m.Version, m.CreatedAt,
                ProcessSteps = m.ModuleProcessSteps
                    .Select(mps => new
                    {
                        mps.ProcessStepId,
                        ProcessStepName = mps.ProcessStep != null ? mps.ProcessStep.Name : null
                    })
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.SlcModules
            .Include(m => m.Project).ThenInclude(p => p!.Software)
            .Include(m => m.Tasks)
            .Include(m => m.ModuleProcessSteps).ThenInclude(mps => mps.ProcessStep)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateModuleDto dto)
    {
        if (await _context.SlcModules.AnyAsync(m => m.Code == dto.Code))
            return BadRequest("Mã module đã tồn tại.");

        var module = new SlcModule
        {
            Code = dto.Code,
            Name = dto.Name,
            ProjectId = dto.ProjectId,
            Description = dto.Description,
            AssigneeCode = dto.AssigneeCode,
            AssigneeName = dto.AssigneeName,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            PlannedProgress = dto.PlannedProgress,
            ActualProgress = dto.ActualProgress,
            Weight = dto.Weight,
            Status = dto.Status,
            Version = dto.Version,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.Now
        };
        _context.SlcModules.Add(module);
        await _context.SaveChangesAsync();

        // Link process steps
        if (dto.ProcessStepIds?.Any() == true)
        {
            foreach (var stepId in dto.ProcessStepIds)
            {
                _context.ModuleProcessSteps.Add(new ModuleProcessStep
                {
                    ModuleId = module.Id,
                    ProcessStepId = stepId
                });
            }
            await _context.SaveChangesAsync();
        }

        return Ok(module);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateModuleDto dto)
    {
        var item = await _context.SlcModules
            .Include(m => m.ModuleProcessSteps)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item == null) return NotFound();

        var previousStart = item.StartDate;
        var previousEnd = item.EndDate;

        item.Name = dto.Name;
        item.ProjectId = dto.ProjectId;
        item.Description = dto.Description;
        item.AssigneeCode = dto.AssigneeCode;
        item.AssigneeName = dto.AssigneeName;
        item.StartDate = dto.StartDate;
        item.EndDate = dto.EndDate;
        item.PlannedProgress = dto.PlannedProgress;
        item.ActualProgress = dto.ActualProgress;
        item.Weight = dto.Weight;
        item.Status = dto.Status;
        item.Version = dto.Version;

        // Log timeline adjustment if dates changed
        if (previousStart != dto.StartDate || previousEnd != dto.EndDate)
        {
            _context.TimelineAdjustments.Add(new TimelineAdjustment
            {
                EntityType = "Module",
                EntityId = id,
                PreviousStartDate = previousStart,
                PreviousEndDate = previousEnd,
                NewStartDate = dto.StartDate,
                NewEndDate = dto.EndDate,
                Reason = "Cập nhật timeline module",
                ChangedAt = DateTime.Now
            });
        }

        // Sync process steps
        if (dto.ProcessStepIds != null)
        {
            _context.ModuleProcessSteps.RemoveRange(item.ModuleProcessSteps);
            foreach (var stepId in dto.ProcessStepIds)
            {
                _context.ModuleProcessSteps.Add(new ModuleProcessStep
                {
                    ModuleId = id,
                    ProcessStepId = stepId
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpPut("{id}/progress")]
    public async Task<IActionResult> UpdateProgress(int id, [FromBody] UpdateProgressDto dto)
    {
        var item = await _context.SlcModules.FindAsync(id);
        if (item == null) return NotFound();
        item.ActualProgress = dto.ActualProgress;
        if (dto.Status.HasValue) item.Status = dto.Status.Value;
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.SlcModules
            .Include(m => m.ModuleProcessSteps)
            .Include(m => m.Tasks)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (item == null) return NotFound();
        _context.ModuleProcessSteps.RemoveRange(item.ModuleProcessSteps);
        _context.SlcTasks.RemoveRange(item.Tasks);
        _context.SlcModules.Remove(item);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class CreateModuleDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string? Description { get; set; }
    public string? AssigneeCode { get; set; }
    public string? AssigneeName { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal PlannedProgress { get; set; } = 0;
    public decimal ActualProgress { get; set; } = 0;
    public decimal Weight { get; set; } = 1;
    public byte Status { get; set; } = 0;
    public string? Version { get; set; }
    public string? CreatedBy { get; set; }
    public List<int>? ProcessStepIds { get; set; }
}

public class UpdateProgressDto
{
    public decimal ActualProgress { get; set; }
    public byte? Status { get; set; }
}
