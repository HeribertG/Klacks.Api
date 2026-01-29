using System.Text;
using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Infrastructure.Services.Translation;

public class DeepLTranslationService : ITranslationService
{
    private readonly HttpClient _httpClient;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ISettingsEncryptionService _encryptionService;
    private readonly ILogger<DeepLTranslationService> _logger;

    private const string SettingsType = "DEEPL_API_KEY";
    private const string DeepLFreeApiUrl = "https://api-free.deepl.com/v2/translate";
    private const string DeepLProApiUrl = "https://api.deepl.com/v2/translate";

    public DeepLTranslationService(
        HttpClient httpClient,
        ISettingsRepository settingsRepository,
        ISettingsEncryptionService encryptionService,
        ILogger<DeepLTranslationService> logger)
    {
        _httpClient = httpClient;
        _settingsRepository = settingsRepository;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public bool IsConfigured => GetApiKeyAsync().GetAwaiter().GetResult() is not null;

    public async Task<TranslationResult> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new TranslationResult(text, sourceLanguage, targetLanguage);
        }

        var apiKey = await GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("DeepL API key is not configured");
        }

        var deepLSourceLang = MapLanguage(sourceLanguage);
        var deepLTargetLang = MapLanguage(targetLanguage);

        var apiUrl = apiKey.EndsWith(":fx") ? DeepLFreeApiUrl : DeepLProApiUrl;

        var requestBody = new Dictionary<string, object>
        {
            { "text", new[] { text } },
            { "source_lang", deepLSourceLang },
            { "target_lang", deepLTargetLang }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"DeepL-Auth-Key {apiKey}");

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DeepLResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var translatedText = result?.Translations?.FirstOrDefault()?.Text ?? text;

            _logger.LogDebug("Translated '{Source}' from {SourceLang} to {TargetLang}: '{Target}'",
                text, sourceLanguage, targetLanguage, translatedText);

            return new TranslationResult(translatedText, sourceLanguage, targetLanguage);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "DeepL API request failed");
            throw new InvalidOperationException("Translation request failed", ex);
        }
    }

    public async Task<Dictionary<string, string>> TranslateToAllLanguagesAsync(string text, string sourceLanguage)
    {
        var results = new Dictionary<string, string>();

        foreach (var targetLang in LanguageConfig.SupportedLanguages)
        {
            if (targetLang.Equals(sourceLanguage, StringComparison.OrdinalIgnoreCase))
            {
                results[targetLang] = text;
            }
            else
            {
                var result = await TranslateAsync(text, sourceLanguage, targetLang);
                results[targetLang] = result.TranslatedText;
            }
        }

        return results;
    }

    private async Task<string?> GetApiKeyAsync()
    {
        var setting = await _settingsRepository.GetSetting(SettingsType);
        if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
        {
            return null;
        }

        return _encryptionService.ProcessForReading(SettingsType, setting.Value);
    }

    private static string MapLanguage(string language)
    {
        return language.ToUpperInvariant();
    }

    private class DeepLResponse
    {
        public List<DeepLTranslation>? Translations { get; set; }
    }

    private class DeepLTranslation
    {
        public string Text { get; set; } = string.Empty;
        public string DetectedSourceLanguage { get; set; } = string.Empty;
    }
}
