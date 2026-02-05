namespace Klacks.Api.Presentation.DTOs.Staffs;

public class AnnotationResource
{
    public Guid ClientId { get; set; }

    public Guid Id { get; set; }

    public string Note { get; set; } = string.Empty;
}
