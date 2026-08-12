using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;
using support.server.Models.SLC;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BusinessProcessController : ControllerBase
{
    private readonly AppDbContext _context;
    public BusinessProcessController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] byte? status, [FromQuery] string? keyword)
    {
        var query = _context.BusinessProcesses
            .Include(bp => bp.Children)
            .Include(bp => bp.ProcessSteps)
            .Where(bp => bp.ParentId == null) // only root level
            .AsQueryable();

        if (status.HasValue) query = query.Where(bp => bp.Status == status.Value);
        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(bp => bp.Name.Contains(keyword) || bp.Code.Contains(keyword));

        var items = await query.OrderBy(bp => bp.OrderIndex).ThenBy(bp => bp.Name).ToListAsync();
        return Ok(items);
    }

    [HttpGet("flat")]
    public async Task<IActionResult> GetFlat([FromQuery] byte? status)
    {
        var query = _context.BusinessProcesses.AsQueryable();
        if (status.HasValue) query = query.Where(bp => bp.Status == status.Value);
        var items = await query.OrderBy(bp => bp.OrderIndex).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.BusinessProcesses
            .Include(bp => bp.Children)
            .Include(bp => bp.ProcessSteps)
            .FirstOrDefaultAsync(bp => bp.Id == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] BusinessProcess dto)
    {
        if (await _context.BusinessProcesses.AnyAsync(bp => bp.Code == dto.Code))
            return BadRequest("Mã quy trình đã tồn tại.");
        dto.Id = 0;
        dto.CreatedAt = DateTime.Now;
        _context.BusinessProcesses.Add(dto);
        await _context.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] BusinessProcess dto)
    {
        var item = await _context.BusinessProcesses.FindAsync(id);
        if (item == null) return NotFound();
        item.Name = dto.Name;
        item.Description = dto.Description;
        item.ParentId = dto.ParentId;
        item.OrderIndex = dto.OrderIndex;
        item.Status = dto.Status;
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.BusinessProcesses.FindAsync(id);
        if (item == null) return NotFound();
        if (await _context.ProcessSteps.AnyAsync(ps => ps.BusinessProcessId == id))
            return BadRequest("Không thể xóa vì đang có bước quy trình liên kết.");
        if (await _context.BusinessProcesses.AnyAsync(bp => bp.ParentId == id))
            return BadRequest("Không thể xóa vì đang có quy trình con.");
        _context.BusinessProcesses.Remove(item);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
