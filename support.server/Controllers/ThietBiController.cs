using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ThietBiController : ControllerBase
    {
        private readonly EPortalDbContext _context;

        public ThietBiController(EPortalDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NvThietBi>>> GetAll(string? keyword = null)
        {
            var query = _context.NvThietBis.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(t => t.TenThietBi != null && t.TenThietBi.Contains(keyword));

            return Ok(await query.OrderBy(t => t.TenThietBi).ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NvThietBi>> GetById(int id)
        {
            var tb = await _context.NvThietBis.FindAsync(id);
            if (tb == null) return NotFound();
            return Ok(tb);
        }

        [HttpPost]
        public async Task<ActionResult<NvThietBi>> Create([FromBody] NvThietBi model)
        {
            _context.NvThietBis.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = model.IdTb }, model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] NvThietBi model)
        {
            var tb = await _context.NvThietBis.FindAsync(id);
            if (tb == null) return NotFound();

            tb.TenThietBi = model.TenThietBi;

            await _context.SaveChangesAsync();
            return Ok(tb);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var tb = await _context.NvThietBis.FindAsync(id);
            if (tb == null) return NotFound();

            _context.NvThietBis.Remove(tb);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
