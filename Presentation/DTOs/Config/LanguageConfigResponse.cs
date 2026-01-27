namespace Klacks.Api.Presentation.DTOs.Config;

public class LanguageConfigResponse
{
    public string[] SupportedLanguages { get; set; } = [];
    public string[] FallbackOrder { get; set; } = [];
}
