using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using support.server.Models;
using support.server.Models.SLC;

namespace support.server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChangeRequestRolesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ChangeRequestRolesController(AppDbContext context) => _context = context;

    [HttpGet("{actorCode}")]
    public async Task<IActionResult> GetByActorCode(string actorCode)
    {
        var roles = await _context.ChangeRequestRoles.AsNoTracking()
            .Where(x => x.IsActive && x.UserCode == actorCode)
            .Select(x => x.RoleCode)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();

        return Ok(new { actorCode, roles });
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddChangeRequestRoleDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ActorCode))
            return BadRequest(new { message = "actorCode là bắt buộc." });
        if (dto.ActorCode.Length > 20)
            return BadRequest(new { message = "actorCode không được vượt quá 20 ký tự." });
        if (string.IsNullOrWhiteSpace(dto.UserCode))
            return BadRequest(new { message = "userCode là bắt buộc." });
        if (dto.UserCode.Length > 20)
            return BadRequest(new { message = "userCode không được vượt quá 20 ký tự." });
        if (string.IsNullOrWhiteSpace(dto.RoleCode))
            return BadRequest(new { message = "roleCode là bắt buộc." });
        if (dto.RoleCode.Length > 20)
            return BadRequest(new { message = "roleCode không được vượt quá 20 ký tự." });
        if (dto.RoleCode.Any(character =>
                character is not (>= 'a' and <= 'z')
                && character is not (>= '0' and <= '9')
                && character != '_'))
            return BadRequest(new { message = "roleCode chỉ được gồm chữ thường, số và dấu gạch dưới." });

        if (!await IsAdminAsync(dto.ActorCode))
            return StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Bạn không có quyền thêm role Change Request." });

        var existing = await _context.ChangeRequestRoles
            .FirstOrDefaultAsync(x => x.UserCode == dto.UserCode && x.RoleCode == dto.RoleCode);
        if (existing != null)
        {
            if (existing.IsActive)
                return Conflict(new { message = "User đã có role Change Request này." });

            existing.IsActive = true;
            existing.CreatedBy = dto.ActorCode;
            existing.CreatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(ToResponse(existing));
        }

        var role = new ChangeRequestRole
        {
            UserCode = dto.UserCode,
            RoleCode = dto.RoleCode,
            IsActive = true,
            CreatedBy = dto.ActorCode,
            CreatedAt = DateTime.Now
        };
        _context.ChangeRequestRoles.Add(role);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (await _context.ChangeRequestRoles.AsNoTracking()
                .AnyAsync(x => x.UserCode == dto.UserCode && x.RoleCode == dto.RoleCode))
                return Conflict(new { message = "User đã có role Change Request này." });
            throw;
        }

        return CreatedAtAction(nameof(GetByActorCode),
            new { actorCode = role.UserCode }, ToResponse(role));
    }

    /// Admin nếu có role_code="admin" đang active trong slc_change_request_roles,
    /// hoặc có permission_code="admin" trong user_permissions.
    private async Task<bool> IsAdminAsync(string actorCode)
    {
        var hasChangeRequestAdminRole = await _context.ChangeRequestRoles.AsNoTracking()
            .AnyAsync(x => x.IsActive && x.UserCode == actorCode && x.RoleCode == ChangeRequestWorkflow.AdminRole);
        if (hasChangeRequestAdminRole) return true;

        return await _context.UserPermissions.AsNoTracking()
            .AnyAsync(p => p.UserCode == actorCode
                && p.PermissionCode == ChangeRequestWorkflow.AdminRole
                && p.IsActive != false);
    }

    private static object ToResponse(ChangeRequestRole role) => new
    {
        role.Id,
        role.UserCode,
        role.RoleCode,
        role.IsActive,
        role.CreatedBy,
        role.CreatedAt
    };
}

public class AddChangeRequestRoleDto
{
    public string ActorCode { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}
