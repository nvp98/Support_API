using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;
using support.server.Models.SLC;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProcessStepController : ControllerBase
{
    private readonly AppDbContext _context;
    public ProcessStepController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? businessProcessId,
        [FromQuery] byte? status)
    {
        var query = _context.ProcessSteps
            .Include(ps => ps.BusinessProcess)
            .AsQueryable();

        if (businessProcessId.HasValue)
            query = query.Where(ps => ps.BusinessProcessId == businessProcessId.Value);
        if (status.HasValue)
            query = query.Where(ps => ps.Status == status.Value);

        var items = await query.OrderBy(ps => ps.OrderIndex).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.ProcessSteps
            .Include(ps => ps.BusinessProcess)
            .FirstOrDefaultAsync(ps => ps.Id == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProcessStep dto)
    {
        dto.Id = 0;
        dto.CreatedAt = DateTime.Now;
        _context.ProcessSteps.Add(dto);
        await _context.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ProcessStep dto)
    {
        var item = await _context.ProcessSteps.FindAsync(id);
        if (item == null) return NotFound();
        item.Name = dto.Name;
        item.Description = dto.Description;
        item.BusinessProcessId = dto.BusinessProcessId;
        item.OrderIndex = dto.OrderIndex;
        item.Status = dto.Status;
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.ProcessSteps.FindAsync(id);
        if (item == null) return NotFound();
        if (await _context.ModuleProcessSteps.AnyAsync(mps => mps.ProcessStepId == id))
            return BadRequest("Không thể xóa vì đang được liên kết với Module.");
        _context.ProcessSteps.Remove(item);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
