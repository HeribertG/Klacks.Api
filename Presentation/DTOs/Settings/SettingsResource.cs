namespace Klacks.Api.Presentation.DTOs.Settings;

public class SettingsResource
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}