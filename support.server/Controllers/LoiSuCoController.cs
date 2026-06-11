using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoiSuCoController : ControllerBase
    {
        private readonly EPortalDbContext _context;

        public LoiSuCoController(EPortalDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NvLoiSuCoTb>>> GetAll(string? keyword = null)
        {
            var query = _context.NvLoiSuCoTbs.AsQueryable();

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(l => l.TenLoiSc != null && l.TenLoiSc.Contains(keyword));

            return Ok(await query.OrderBy(l => l.TenLoiSc).ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NvLoiSuCoTb>> GetById(int id)
        {
            var lsc = await _context.NvLoiSuCoTbs.FindAsync(id);
            if (lsc == null) return NotFound();
            return Ok(lsc);
        }

        [HttpPost]
        public async Task<ActionResult<NvLoiSuCoTb>> Create([FromBody] NvLoiSuCoTb model)
        {
            _context.NvLoiSuCoTbs.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = model.IdSc }, model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] NvLoiSuCoTb model)
        {
            var lsc = await _context.NvLoiSuCoTbs.FindAsync(id);
            if (lsc == null) return NotFound();

            lsc.TenLoiSc = model.TenLoiSc;

            await _context.SaveChangesAsync();
            return Ok(lsc);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var lsc = await _context.NvLoiSuCoTbs.FindAsync(id);
            if (lsc == null) return NotFound();

            _context.NvLoiSuCoTbs.Remove(lsc);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
