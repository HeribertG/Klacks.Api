namespace Klacks.Api.Presentation.DTOs.Translation;

public class TranslationRequest
{
    public string Text { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = "de";
}
