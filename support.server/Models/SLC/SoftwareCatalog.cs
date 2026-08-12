using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_software")]
public class SoftwareCatalog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [MaxLength(20)]
    public string Code { get; set; } = string.Empty;

    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("status")]
    public byte Status { get; set; } = 1; // 1=Active, 0=Inactive

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    [MaxLength(20)]
    public string? CreatedBy { get; set; }

    public ICollection<SlcProject> Projects { get; set; } = new List<SlcProject>();
}
