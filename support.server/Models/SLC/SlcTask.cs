using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_tasks")]
public class SlcTask
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

    [Column("module_id")]
    public int ModuleId { get; set; }

    [Column("description")]
    [MaxLength(1000)]
    public string? Description { get; set; }

    [Column("assignee_code")]
    [MaxLength(20)]
    public string? AssigneeCode { get; set; }

    [Column("assignee_name")]
    [MaxLength(100)]
    public string? AssigneeName { get; set; }

    [Column("start_date", TypeName = "date")]
    public DateOnly? StartDate { get; set; }

    [Column("end_date", TypeName = "date")]
    public DateOnly? EndDate { get; set; }

    // 1=Low, 2=Medium, 3=High, 4=Critical
    [Column("priority")]
    public byte Priority { get; set; } = 2;

    // 0=NotStarted, 1=InProgress, 2=Completed, 3=Blocked
    [Column("status")]
    public byte Status { get; set; } = 0;

    [Column("progress", TypeName = "decimal(5,2)")]
    public decimal Progress { get; set; } = 0;

    [Column("estimated_hours", TypeName = "decimal(6,2)")]
    public decimal? EstimatedHours { get; set; }

    [Column("actual_hours", TypeName = "decimal(6,2)")]
    public decimal? ActualHours { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    [MaxLength(20)]
    public string? CreatedBy { get; set; }

    [ForeignKey("ModuleId")]
    public SlcModule? Module { get; set; }
}
