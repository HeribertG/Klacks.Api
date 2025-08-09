using Klacks.Api.Presentation.Resources.Schedules;
using Klacks.Api.Presentation.Resources.Staffs;
using System.Text.Json.Serialization;

namespace Klacks.Api.Presentation.Resources.Associations;

public class GroupItemResource
{
    public ClientResource? Client { get; set; }

    public Guid? ClientId { get; set; }

    public Guid? ShiftId { get; set; }

    public Guid GroupId { get; set; }

    public Guid Id { get; set; }

      public GroupResource? Group { get; set; }

      public ShiftResource? Shift { get; set; } = null!;
}