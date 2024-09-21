using Klacks.Api.Models.Schedules;
using Klacks.Api.Resources.Staffs;

namespace Klacks.Api.Resources.Schedules
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
