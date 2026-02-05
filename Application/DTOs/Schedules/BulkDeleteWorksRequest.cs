namespace Klacks.Api.Application.DTOs.Schedules;

public class BulkDeleteWorksRequest
{
    public List<Guid> WorkIds { get; set; } = [];
}
