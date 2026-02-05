namespace Klacks.Api.Presentation.DTOs.Schedules;

public class BulkDeleteWorksRequest
{
    public List<Guid> WorkIds { get; set; } = [];
}
