using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_projects")]
public class SlcProject
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

    [Column("software_id")]
    public int SoftwareId { get; set; }

    [Column("description")]
    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column("start_date", TypeName = "date")]
    public DateOnly? StartDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateOnly? EndDate { get; set; }

    [Column("go_live_date", TypeName = "date")]
    public DateOnly? GoLiveDate { get; set; }

    [Column("version")]
    [MaxLength(20)]
    public string? Version { get; set; }

    // 0=Planning, 1=InProgress, 2=Completed, 3=OnHold, 4=Cancelled
    [Column("status")]
    public byte Status { get; set; } = 0;

    [Column("weight", TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    [MaxLength(20)]
    public string? CreatedBy { get; set; }

    [ForeignKey("SoftwareId")]
    public SoftwareCatalog? Software { get; set; }

    public ICollection<SlcModule> Modules { get; set; } = new List<SlcModule>();
    public ICollection<ChangeRequest> ChangeRequests { get; set; } = new List<ChangeRequest>();
}
