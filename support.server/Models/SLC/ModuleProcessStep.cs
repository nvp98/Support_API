using System.ComponentModel.DataAnnotations.Schema;

namespace support.server.Models.SLC;

[Table("slc_module_process_steps")]
public class ModuleProcessStep
{
    [Column("module_id")]
    public int ModuleId { get; set; }

    [Column("process_step_id")]
    public int ProcessStepId { get; set; }

    [ForeignKey("ModuleId")]
    public SlcModule? Module { get; set; }

    [ForeignKey("ProcessStepId")]
    public ProcessStep? ProcessStep { get; set; }
}
