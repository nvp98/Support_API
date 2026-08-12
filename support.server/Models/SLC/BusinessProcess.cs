using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_business_processes")]
public class BusinessProcess
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

    [Column("parent_id")]
    public int? ParentId { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; } = 0;

    [Column("status")]
    public byte Status { get; set; } = 1; // 1=Active, 0=Inactive

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("ParentId")]
    public BusinessProcess? Parent { get; set; }

    public ICollection<BusinessProcess> Children { get; set; } = new List<BusinessProcess>();
    public ICollection<ProcessStep> ProcessSteps { get; set; } = new List<ProcessStep>();
}
