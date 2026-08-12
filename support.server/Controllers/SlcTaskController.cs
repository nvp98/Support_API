using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;
using support.server.Models.SLC;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SlcTaskController : ControllerBase
{
    private readonly AppDbContext _context;
    public SlcTaskController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? moduleId,
        [FromQuery] byte? status,
        [FromQuery] string? assigneeCode,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.SlcTasks
            .Include(t => t.Module)
            .AsQueryable();

        if (moduleId.HasValue) query = query.Where(t => t.ModuleId == moduleId.Value);
        if (status.HasValue) query = query.Where(t => t.Status == status.Value);
        if (!string.IsNullOrEmpty(assigneeCode))
            query = query.Where(t => t.AssigneeCode == assigneeCode);
        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(t => t.Name.Contains(keyword));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(t => t.Status).ThenBy(t => t.EndDate)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.SlcTasks
            .Include(t => t.Module)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SlcTask dto)
    {
        dto.Id = 0;
        dto.CreatedAt = DateTime.Now;
        _context.SlcTasks.Add(dto);
        await _context.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SlcTask dto)
    {
        var item = await _context.SlcTasks.FindAsync(id);
        if (item == null) return NotFound();

        var previousStart = item.StartDate;
        var previousEnd = item.EndDate;

        item.Name = dto.Name;
        item.ModuleId = dto.ModuleId;
        item.Description = dto.Description;
        item.AssigneeCode = dto.AssigneeCode;
        item.AssigneeName = dto.AssigneeName;
        item.StartDate = dto.StartDate;
        item.EndDate = dto.EndDate;
        item.Priority = dto.Priority;
        item.Status = dto.Status;
        item.Progress = dto.Progress;
        item.EstimatedHours = dto.EstimatedHours;
        item.ActualHours = dto.ActualHours;

        if (previousStart != dto.StartDate || previousEnd != dto.EndDate)
        {
            _context.TimelineAdjustments.Add(new TimelineAdjustment
            {
                EntityType = "Task",
                EntityId = id,
                PreviousStartDate = previousStart,
                PreviousEndDate = previousEnd,
                NewStartDate = dto.StartDate,
                NewEndDate = dto.EndDate,
                Reason = "Cập nhật timeline task",
                ChangedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.SlcTasks.FindAsync(id);
        if (item == null) return NotFound();
        _context.SlcTasks.Remove(item);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
