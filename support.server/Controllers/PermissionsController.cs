using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PermissionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Permision>>> GetAll()
            => await _context.Permisions.ToListAsync();

        [HttpGet("{code}")]
        public async Task<ActionResult<Permision>> GetByCode(string code)
        {
            var item = await _context.Permisions.FindAsync(code);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        public async Task<ActionResult<Permision>> Create(Permision model)
        {
            _context.Permisions.Add(model);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetByCode), new { code = model.PermisonCode }, model);
        }

        [HttpPut("{code}")]
        public async Task<IActionResult> Update(string code, Permision model)
        {
            if (code != model.PermisonCode) return BadRequest();
            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{code}")]
        public async Task<IActionResult> Delete(string code)
        {
            var item = await _context.Permisions.FindAsync(code);
            if (item == null) return NotFound();
            _context.Permisions.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
