using Klacks_api.Models.Schedules;
using Klacks_api.Resources.Staffs;

namespace Klacks_api.Resources.Schedules
{
  public class WorkResource
  {
    public ClientResource? Client { get; set; }

    public Guid ClientId { get; set; }

    public DateTime From { get; set; }

    public Guid Id { get; set; }

    public string? Information { get; set; }

    public bool IsSealed { get; set; }

    public Shift? Shift { get; set; }

    public Guid ShiftId { get; set; }

    public DateTime Until { get; set; }
  }
}
