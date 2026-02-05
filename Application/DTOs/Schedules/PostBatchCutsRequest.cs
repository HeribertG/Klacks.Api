using Klacks.Api.Application.Commands.Shifts;

namespace Klacks.Api.Presentation.DTOs.Schedules;

public class PostBatchCutsRequest
{
    public List<CutOperation> Operations { get; set; } = new();
}
