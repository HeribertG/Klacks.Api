using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Shifts;

public class CutOperation
{
    public string Type { get; set; } = string.Empty;

    public string ParentId { get; set; } = string.Empty;

    public ShiftResource Data { get; set; } = null!;
}
