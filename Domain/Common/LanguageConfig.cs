namespace Klacks.Api.Domain.Common;

public static class LanguageConfig
{
    private static string[] _supportedLanguages = ["de", "en", "fr", "it"];
    private static string[] _fallbackOrder = ["de", "fr", "it", "en"];

    public static string[] SupportedLanguages => _supportedLanguages;

    public static string[] FallbackOrder => _fallbackOrder;

    public static void Configure(string[] supportedLanguages, string[]? fallbackOrder = null)
    {
        _supportedLanguages = supportedLanguages;
        if (fallbackOrder != null)
        {
            _fallbackOrder = fallbackOrder;
        }
    }
}
