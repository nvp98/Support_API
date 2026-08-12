using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_modules")]
public class SlcModule
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

    [Column("project_id")]
    public int ProjectId { get; set; }

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

    [Column("planned_progress", TypeName = "decimal(5,2)")]
    public decimal PlannedProgress { get; set; } = 0;

    [Column("actual_progress", TypeName = "decimal(5,2)")]
    public decimal ActualProgress { get; set; } = 0;

    [Column("weight", TypeName = "decimal(5,2)")]
    public decimal Weight { get; set; } = 1;

    // 0=NotStarted, 1=InProgress, 2=Completed, 3=OnHold
    [Column("status")]
    public byte Status { get; set; } = 0;

    [Column("version")]
    [MaxLength(20)]
    public string? Version { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("created_by")]
    [MaxLength(20)]
    public string? CreatedBy { get; set; }

    [ForeignKey("ProjectId")]
    public SlcProject? Project { get; set; }

    public ICollection<SlcTask> Tasks { get; set; } = new List<SlcTask>();
    public ICollection<ModuleProcessStep> ModuleProcessSteps { get; set; } = new List<ModuleProcessStep>();
    public ICollection<ChangeRequest> ChangeRequests { get; set; } = new List<ChangeRequest>();
}
