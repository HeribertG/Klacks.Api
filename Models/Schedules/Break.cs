using Klacks_api.Datas;
using Klacks_api.Models.Staffs;


namespace Klacks_api.Models.Schedules;

public class Break : BaseEntity
{

  public Guid AbsenceId { get; set; }

    public virtual Absence Absence { get; set; } = null!;

    public Guid ClientId { get; set; }

    public virtual Client Client { get; set; } = null!;

    public DateTime From { get; set; }

  public string? Information { get; set; }

  public DateTime Until { get; set; }

  public Guid BreakReasonId { get; set; }
  public BreakReason? BreakReason { get; set; }
}
