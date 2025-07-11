namespace Klacks.Api.Resources.Associations;

public class SimpleGroupResource
{
    public string Description { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidUntil { get; set; }
}
