using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_change_request_roles")]
public class ChangeRequestRole
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("user_code")]
    [MaxLength(20)]
    public string UserCode { get; set; } = string.Empty;

    [Column("role_code")]
    [MaxLength(20)]
    public string RoleCode { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    [MaxLength(20)]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
