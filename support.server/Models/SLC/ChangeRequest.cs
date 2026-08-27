using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_change_requests")]
public class ChangeRequest
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("code")]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty; // REQ-YYMMDD-XXXX

    [Column("title")]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string? Content { get; set; }

    [Column("before_change_content")]
    public string? BeforeChangeContent { get; set; }

    [Column("reason")]
    [MaxLength(1000)]
    public string? Reason { get; set; }

    [Column("file_attachments")]
    [MaxLength(500)]
    public string? FileAttachments { get; set; }

    // 1=Low, 2=Medium, 3=High, 4=Critical
    [Column("priority")]
    public byte Priority { get; set; } = 2;

    [Column("module_id")]
    public int? ModuleId { get; set; }

    [Column("project_id")]
    public int? ProjectId { get; set; }

    [Column("requestor_code")]
    [MaxLength(20)]
    public string? RequestorCode { get; set; }

    [Column("requestor_name")]
    [MaxLength(100)]
    public string? RequestorName { get; set; }

    [Column("requestor_dept")]
    [MaxLength(100)]
    public string? RequestorDept { get; set; }

    [Column("approver_code")]
    [MaxLength(20)]
    public string? ApproverCode { get; set; }

    [Column("approver_name")]
    [MaxLength(100)]
    public string? ApproverName { get; set; }

    [Column("developer_code")]
    [MaxLength(20)]
    public string? DeveloperCode { get; set; }

    [Column("developer_name")]
    [MaxLength(100)]
    public string? DeveloperName { get; set; }

    // 0=Bản nháp, 1=Chờ tiếp nhận, 2=Chờ xác nhận,
    // 3=Chờ hoàn thành, 4=Hoàn thành, 5=Từ chối
    [Column("status")]
    public byte Status { get; set; } = 0;

    [Column("impact_timeline")]
    public bool ImpactTimeline { get; set; } = false;

    [Column("impact_days")]
    public int ImpactDays { get; set; } = 0;

    [Column("impact_version")]
    [MaxLength(50)]
    public string? ImpactVersion { get; set; }

    [Column("impact_modules")]
    [MaxLength(500)]
    public string? ImpactModules { get; set; }

    [Column("current_revision")]
    public int CurrentRevision { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by_code")]
    [MaxLength(20)]
    public string? CreatedByCode { get; set; }

    [Column("created_by_name")]
    [MaxLength(100)]
    public string? CreatedByName { get; set; }

    [Column("submitted_at")]
    public DateTime? SubmittedAt { get; set; }

    [Column("submitted_by_code")]
    [MaxLength(20)]
    public string? SubmittedByCode { get; set; }

    [Column("submitted_by_name")]
    [MaxLength(100)]
    public string? SubmittedByName { get; set; }

    [Column("developer_accepted_at")]
    public DateTime? DeveloperAcceptedAt { get; set; }

    [Column("expected_completion_date", TypeName = "date")]
    public DateOnly? ExpectedCompletionDate { get; set; }

    [Column("approved_at")]
    public DateTime? ApprovedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("completed_by_code")]
    [MaxLength(20)]
    public string? CompletedByCode { get; set; }

    [Column("completed_by_name")]
    [MaxLength(100)]
    public string? CompletedByName { get; set; }

    [Column("rejected_at")]
    public DateTime? RejectedAt { get; set; }

    [Column("rejected_reason")]
    [MaxLength(500)]
    public string? RejectedReason { get; set; }

    [Timestamp]
    [Column("row_version")]
    public byte[] RowVersion { get; set; } = [];

    [ForeignKey("ModuleId")]
    public SlcModule? Module { get; set; }

    [ForeignKey("ProjectId")]
    public SlcProject? Project { get; set; }

    public ICollection<ChangeRevision> Revisions { get; set; } = new List<ChangeRevision>();

    [NotMapped]
    public string StatusName => ChangeRequestWorkflow.GetStatusName(Status);

    [NotMapped]
    public IReadOnlyList<string> AllowedActions { get; set; } = [];

    [NotMapped]
    public string? ModuleName => Module?.Name;

    [NotMapped]
    public string? ProjectName => Project?.Name;
}
