using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_timeline_adjustments")]
public class TimelineAdjustment
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    // 'Project', 'Module', 'Task'
    [Column("entity_type")]
    [MaxLength(20)]
    public string EntityType { get; set; } = string.Empty;

    [Column("entity_id")]
    public int EntityId { get; set; }

    [Column("previous_start_date", TypeName = "date")]
    public DateOnly? PreviousStartDate { get; set; }

    [Column("previous_end_date", TypeName = "date")]
    public DateOnly? PreviousEndDate { get; set; }

    [Column("new_start_date", TypeName = "date")]
    public DateOnly? NewStartDate { get; set; }

    [Column("new_end_date", TypeName = "date")]
    public DateOnly? NewEndDate { get; set; }

    [Column("adjustment_days")]
    public int AdjustmentDays { get; set; } = 0;

    [Column("reason")]
    [MaxLength(500)]
    public string? Reason { get; set; }

    [Column("change_request_id")]
    public int? ChangeRequestId { get; set; }

    [Column("changed_by")]
    [MaxLength(20)]
    public string? ChangedBy { get; set; }

    [Column("changed_by_name")]
    [MaxLength(100)]
    public string? ChangedByName { get; set; }

    [Column("changed_at")]
    public DateTime ChangedAt { get; set; } = DateTime.Now;

    [ForeignKey("ChangeRequestId")]
    public ChangeRequest? ChangeRequest { get; set; }
}
