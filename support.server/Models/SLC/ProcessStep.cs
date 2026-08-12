using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_process_steps")]
public class ProcessStep
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

    [Column("business_process_id")]
    public int BusinessProcessId { get; set; }

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; } = 0;

    [Column("status")]
    public byte Status { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey("BusinessProcessId")]
    public BusinessProcess? BusinessProcess { get; set; }

    public ICollection<ModuleProcessStep> ModuleProcessSteps { get; set; } = new List<ModuleProcessStep>();
}
