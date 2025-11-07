using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;

namespace support.server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserPermissionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserPermissionsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserPermission>>> GetAll()
            => await _context.UserPermissions.ToListAsync();

        [HttpGet("{MaNV}")]
        public async Task<ActionResult<UserPermission>> GetById(string? MaNV)
        {
            var item = await _context.UserPermissions.FirstOrDefaultAsync(x=>x.UserCode == MaNV);
            if (item == null) return Ok(null);
            return item;
        }

        [HttpPost]
        public async Task<IActionResult> Create(UserPermission model)
        {
            var oldpermission = await _context.UserPermissions.Where(x => x.UserCode == model.UserCode).ExecuteDeleteAsync();
            _context.UserPermissions.Add(model);
            await _context.SaveChangesAsync();
            return Created("Thành công",model);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UserPermission model)
        {
            if (id != model.PermissionId) return BadRequest();
            _context.Entry(model).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _context.UserPermissions.FindAsync(id);
            if (item == null) return NotFound();
            _context.UserPermissions.Remove(item);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

}
