using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;
using support.server.Models.SLC;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SoftwareCatalogController : ControllerBase
{
    private readonly AppDbContext _context;
    public SoftwareCatalogController(AppDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] byte? status, [FromQuery] string? keyword)
    {
        var query = _context.SoftwareCatalogs.AsQueryable();
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (!string.IsNullOrEmpty(keyword))
            query = query.Where(x => x.Name.Contains(keyword) || x.Code.Contains(keyword));

        var items = await query.OrderBy(x => x.Name).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _context.SoftwareCatalogs
            .Include(x => x.Projects)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SoftwareCatalog dto)
    {
        if (await _context.SoftwareCatalogs.AnyAsync(x => x.Code == dto.Code))
            return BadRequest("Mã phần mềm đã tồn tại.");
        dto.Id = 0;
        dto.CreatedAt = DateTime.Now;
        _context.SoftwareCatalogs.Add(dto);
        await _context.SaveChangesAsync();
        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] SoftwareCatalog dto)
    {
        var item = await _context.SoftwareCatalogs.FindAsync(id);
        if (item == null) return NotFound();
        item.Name = dto.Name;
        item.Description = dto.Description;
        item.Status = dto.Status;
        await _context.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.SoftwareCatalogs.FindAsync(id);
        if (item == null) return NotFound();
        if (await _context.SlcProjects.AnyAsync(p => p.SoftwareId == id))
            return BadRequest("Không thể xóa vì đang có dự án liên kết.");
        _context.SoftwareCatalogs.Remove(item);
        await _context.SaveChangesAsync();
        return Ok();
    }
}
