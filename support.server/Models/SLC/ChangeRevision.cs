using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_change_revisions")]
public class ChangeRevision
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("change_request_id")]
    public int ChangeRequestId { get; set; }

    [Column("revision_number")]
    public int RevisionNumber { get; set; } = 0; // Rev0, Rev1, Rev2...

    [Column("content")]
    public string? Content { get; set; }

    [Column("reason")]
    [MaxLength(1000)]
    public string? Reason { get; set; }

    [Column("requestor_code")]
    [MaxLength(20)]
    public string? RequestorCode { get; set; }

    [Column("requestor_name")]
    [MaxLength(100)]
    public string? RequestorName { get; set; }

    [Column("approver_code")]
    [MaxLength(20)]
    public string? ApproverCode { get; set; }

    [Column("approver_name")]
    [MaxLength(100)]
    public string? ApproverName { get; set; }

    [Column("impact_assessment")]
    [MaxLength(1000)]
    public string? ImpactAssessment { get; set; }

    [Column("impact_timeline")]
    public bool ImpactTimeline { get; set; } = false;

    [Column("impact_days")]
    public int ImpactDays { get; set; } = 0;

    [Column("impact_version")]
    [MaxLength(50)]
    public string? ImpactVersion { get; set; }

    // 0=Pending, 1=Approved, 2=Rejected
    [Column("status")]
    public byte Status { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [ForeignKey("ChangeRequestId")]
    public ChangeRequest? ChangeRequest { get; set; }
}
