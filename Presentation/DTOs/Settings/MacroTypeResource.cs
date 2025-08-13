namespace Klacks.Api.Presentation.DTOs.Settings;

public class MacroTypeResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public int Type { get; set; }
}