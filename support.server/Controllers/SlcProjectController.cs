using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;
using support.server.Models.SLC;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SlcProjectController : ControllerBase
{
    private readonly AppDbContext _context;
    public SlcProjectController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? softwareId,
        [FromQuery] byte? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = _context.SlcProjects
            .Include(p => p.Software)
            .AsQueryable();

        if (softwareId.HasValue) query = query.Where(p => p.SoftwareId == softwareId.Value);
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(p => p.Name.Contains(keyword) || p.Code.Contains(keyword));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new
            {
                p.Id, p.Code, p.Name, p.SoftwareId,
                SoftwareName = p.Software != null ? p.Software.Name : null,
                p.Description, p.StartDate, p.EndDate, p.GoLiveDate,
                p.Version, p.Status, p.Weight, p.CreatedAt,
                ModuleCount = p.Modules.Count()
            })
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.SlcProjects
            .Include(p => p.Software)
            .Include(p => p.Modules)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpGet("{id}/progress")]
    public async Task<IActionResult> GetProgress(int id)
    {
        var modules = await _context.SlcModules
            .Where(m => m.ProjectId == id)
            .ToListAsync();

        if (!modules.Any()) return Ok(new { progress = 0, totalWeight = 0 });

        var totalWeight = modules.Sum(m => m.Weight);
        var weightedProgress = totalWeight > 0
            ? modules.Sum(m => m.ActualProgress * m.Weight) / totalWeight
            : 0;

        return Ok(new
        {
            progress = Math.Round(weightedProgress, 2),
            totalWeight,
            moduleCount = modules.Count,
            completedModules = modules.Count(m => m.Status == 2)
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SlcProject dto)
    {
        if (await _context.SlcProjects.AnyAsync(p => p.Code == dto.Code))
            return BadRequest("Mã dự án đã tồn tại.");
        dto.Id = 0;
        dto.CreatedAt = DateTime.Now;
        _context.SlcProjects.Add(dto);
        await _context.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SlcProject dto)
    {
        var item = await _context.SlcProjects.FindAsync(id);
        if (item == null) return NotFound();

        var previousStart = item.StartDate;
        var previousEnd = item.EndDate;

        item.Name = dto.Name;
        item.SoftwareId = dto.SoftwareId;
        item.Description = dto.Description;
        item.StartDate = dto.StartDate;
        item.EndDate = dto.EndDate;
        item.GoLiveDate = dto.GoLiveDate;
        item.Version = dto.Version;
        item.Status = dto.Status;
        item.Weight = dto.Weight;

        // Log timeline adjustment if dates changed
        if (previousStart != dto.StartDate || previousEnd != dto.EndDate)
        {
            _context.TimelineAdjustments.Add(new TimelineAdjustment
            {
                EntityType = "Project",
                EntityId = id,
                PreviousStartDate = previousStart,
                PreviousEndDate = previousEnd,
                NewStartDate = dto.StartDate,
                NewEndDate = dto.EndDate,
                Reason = "Cập nhật timeline dự án",
                ChangedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.SlcProjects.FindAsync(id);
        if (item == null) return NotFound();
        if (await _context.SlcModules.AnyAsync(m => m.ProjectId == id))
            return BadRequest("Không thể xóa vì đang có module liên kết.");
        _context.SlcProjects.Remove(item);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
