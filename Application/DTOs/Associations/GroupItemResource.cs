using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Staffs;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Presentation.DTOs.Associations;

public class GroupItemResource
{
    public ClientResource? Client { get; set; }

    public Guid? ClientId { get; set; }

    public Guid? ShiftId { get; set; }

    public Guid GroupId { get; set; }

    public Guid Id { get; set; }

    public GroupResource? Group { get; set; }

    public ShiftResource? Shift { get; set; } = null!;

    [DataType(DataType.Date)]
    public DateTime? ValidFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? ValidUntil { get; set; }
}