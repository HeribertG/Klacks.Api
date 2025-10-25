namespace Klacks.Api.Presentation.DTOs.Schedules;

public class PostResetCutsRequest
{
    public Guid OriginalId { get; set; }

    public DateTime NewStartDate { get; set; }
}
