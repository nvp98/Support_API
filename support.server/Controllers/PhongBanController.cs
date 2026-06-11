using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PhongBanController : ControllerBase
    {
        private readonly EPortalDbContext _context;

        public PhongBanController(EPortalDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PhongBan>>> GetAll(int? status = null)
        {
            var query = _context.PhongBans.AsQueryable();

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            return Ok(await query.OrderBy(p => p.TenPhongBan).ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PhongBan>> GetById(int id)
        {
            var pb = await _context.PhongBans.FindAsync(id);
            if (pb == null) return NotFound();
            return Ok(pb);
        }

        [HttpPost]
        public async Task<ActionResult<PhongBan>> Create([FromBody] PhongBan model)
        {
            model.Status ??= 1;
            _context.PhongBans.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = model.IdPhongBan }, model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PhongBan model)
        {
            var pb = await _context.PhongBans.FindAsync(id);
            if (pb == null) return NotFound();

            pb.TenPhongBan = model.TenPhongBan;
            pb.Status = model.Status;

            await _context.SaveChangesAsync();
            return Ok(pb);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var pb = await _context.PhongBans.FindAsync(id);
            if (pb == null) return NotFound();

            _context.PhongBans.Remove(pb);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
