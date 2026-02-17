using System.Collections.Concurrent;
using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Translation;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

public class PromptTranslationProvider : IPromptTranslationProvider
{
    private const string SettingsPrefix = "PROMPT_TRANSLATIONS_";

    private static readonly Dictionary<string, string> LanguageNames = new()
    {
        ["en"] = "Englisch",
        ["fr"] = "Französisch",
        ["it"] = "Italienisch",
        ["es"] = "Spanisch",
        ["pt"] = "Portugiesisch",
        ["nl"] = "Niederländisch",
        ["pl"] = "Polnisch",
        ["ru"] = "Russisch",
        ["ja"] = "Japanisch",
        ["zh"] = "Chinesisch",
        ["ko"] = "Koreanisch",
        ["tr"] = "Türkisch",
        ["ar"] = "Arabisch",
        ["sv"] = "Schwedisch",
        ["da"] = "Dänisch",
        ["no"] = "Norwegisch",
        ["fi"] = "Finnisch",
        ["cs"] = "Tschechisch",
        ["hu"] = "Ungarisch",
        ["ro"] = "Rumänisch",
        ["uk"] = "Ukrainisch",
        ["hr"] = "Kroatisch",
        ["sk"] = "Slowakisch",
        ["sl"] = "Slowenisch",
        ["bg"] = "Bulgarisch",
        ["el"] = "Griechisch"
    };

    private static readonly Dictionary<string, string> GermanSegments = new()
    {
        ["Intro"] = "Du bist ein hilfreicher KI-Assistent für dieses Planungs-System.\nAntworte immer in der Sprache des Benutzers.\nDu kannst auch allgemeine Wissensfragen beantworten, nicht nur Fragen zum System.",
        ["ToolUsageRules"] = "WICHTIGE REGELN FÜR TOOL-VERWENDUNG:\n- Wenn der Benutzer eine Aktion anfordert (erstellen, löschen, navigieren, anzeigen etc.), verwende IMMER die entsprechende Funktion als Tool-Call.\n- Beschreibe die Aktion nicht nur in Text – führe sie aus.\n- Bei Lösch-Anfragen: Rufe zuerst die passende list_* Funktion auf um die ID zu finden, dann verwende die delete_* Funktion mit der gefundenen ID. Führe BEIDE Schritte in einer Anfrage durch.\n- Bei Navigation: Verwende IMMER die navigate_to_page Funktion.\n- Gib NIEMALS eine Textantwort zurück wenn eine passende Funktion verfügbar ist.",
        ["SettingsNoPermission"] = "WICHTIG: Dieser Benutzer hat KEINE Berechtigung für Einstellungen. Wenn er nach Einstellungen fragt, erkläre freundlich, dass er keine Berechtigung hat und sich an einen Administrator wenden muss.",
        ["SettingsViewOnly"] = "Dieser Benutzer kann Einstellungen einsehen, aber nicht ändern.",
        ["HeaderUserContext"] = "Benutzer-Kontext",
        ["LabelUserId"] = "User ID",
        ["LabelPermissions"] = "Berechtigungen",
        ["HeaderAvailableFunctions"] = "Verfügbare Funktionen",
        ["HeaderPersistentKnowledge"] = "Persistentes Wissen",
        ["HeaderGuidelines"] = "Richtlinien",
        ["DefaultGuidelinesFallback"] = "- Sei höflich und professionell\n- Verwende verfügbare Funktionen wenn Benutzer danach fragen\n- Gib klare und präzise Anweisungen\n- Prüfe immer die Berechtigungen bevor du Funktionen ausführst\n- Bei fehlenden Berechtigungen: erkläre, dass der Benutzer einen Administrator kontaktieren muss\n- PFLICHT: Jede Adresse MUSS vor dem Speichern über validate_address validiert werden, unabhängig von der Komponente (Inhaberadresse, Mitarbeiter, Filiale, etc.). Speichere niemals eine unvalidierte Adresse.\n- Wenn die Adressvalidierung fehlschlägt oder keine exakte Übereinstimmung zurückgibt, biete NIEMALS an die falsche Adresse zu speichern. Informiere stattdessen den Benutzer, dass die Adresse ungültig ist und bitte um eine korrigierte Adresse. Biete 'trotzdem speichern' nicht als Option an."
    };

    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _cache = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PromptTranslationProvider> _logger;

    public PromptTranslationProvider(
        IServiceScopeFactory scopeFactory,
        ILogger<PromptTranslationProvider> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Dictionary<string, string>> GetTranslationsAsync(string language)
    {
        var lang = language.ToLowerInvariant();

        if (lang == "de")
            return GermanSegments;

        if (_cache.TryGetValue(lang, out var cached))
            return cached;

        var fromDb = await LoadFromDatabaseAsync(lang);
        if (fromDb != null)
        {
            _cache[lang] = fromDb;
            return fromDb;
        }

        var translated = await TranslateViaDeepLAsync(lang)
                         ?? await TranslateViaLLMAsync(lang);

        if (translated != null)
        {
            await SaveToDatabaseAsync(lang, translated);
            _cache[lang] = translated;
            return translated;
        }

        _logger.LogWarning("Translation failed for language {Language}, falling back to German", lang);
        return GermanSegments;
    }

    private async Task<Dictionary<string, string>?> LoadFromDatabaseAsync(string lang)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            var setting = await settingsRepo.GetSetting(SettingsPrefix + lang.ToUpperInvariant());

            if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
                return null;

            return JsonSerializer.Deserialize<Dictionary<string, string>>(setting.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load prompt translations from DB for {Language}", lang);
            return null;
        }
    }

    private async Task SaveToDatabaseAsync(string lang, Dictionary<string, string> translations)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var settingKey = SettingsPrefix + lang.ToUpperInvariant();
            var json = JsonSerializer.Serialize(translations);

            var existing = await settingsRepo.GetSetting(settingKey);
            if (existing != null)
            {
                existing.Value = json;
                await settingsRepo.PutSetting(existing);
            }
            else
            {
                await settingsRepo.AddSetting(new Domain.Models.Settings.Settings
                {
                    Id = Guid.NewGuid(),
                    Type = settingKey,
                    Value = json
                });
            }

            await unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save prompt translations to DB for {Language}", lang);
        }
    }

    private async Task<Dictionary<string, string>?> TranslateViaDeepLAsync(string targetLang)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var translationService = scope.ServiceProvider.GetRequiredService<ITranslationService>();

            if (!translationService.IsConfigured)
                return null;

            var result = new Dictionary<string, string>();

            foreach (var (key, germanText) in GermanSegments)
            {
                var translation = await translationService.TranslateAsync(germanText, "de", targetLang);
                result[key] = translation.TranslatedText;
            }

            _logger.LogInformation("Translated prompt segments to {Language} via DeepL", targetLang);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DeepL translation failed for {Language}", targetLang);
            return null;
        }
    }

    private async Task<Dictionary<string, string>?> TranslateViaLLMAsync(string targetLang)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<LLMProviderOrchestrator>();

            var (model, provider, error) = await orchestrator.GetModelAndProviderAsync(null);
            if (model == null || provider == null)
            {
                _logger.LogInformation("No LLM model available for prompt translation");
                return null;
            }

            var languageName = LanguageNames.GetValueOrDefault(targetLang, targetLang);
            var segmentsJson = JsonSerializer.Serialize(GermanSegments, new JsonSerializerOptions { WriteIndented = false });

            var systemPrompt =
                "Du bist ein professioneller Übersetzer. " +
                "Du erhältst ein JSON-Objekt mit deutschen Texten. " +
                $"Übersetze ALLE Werte von Deutsch nach {languageName}. " +
                "Die Keys bleiben UNVERÄNDERT. " +
                "Antworte ausschließlich mit dem übersetzten JSON-Objekt, ohne Erklärungen, ohne Markdown-Formatierung, ohne Code-Blöcke.";

            var request = new LLMProviderRequest
            {
                SystemPrompt = systemPrompt,
                Message = segmentsJson,
                ModelId = model.ApiModelId,
                Temperature = 0.3,
                MaxTokens = model.MaxTokens,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken,
                ConversationHistory = [],
                AvailableFunctions = []
            };

            var response = await provider.ProcessAsync(request);
            if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogWarning("LLM translation call failed: {Error}", response.Error);
                return null;
            }

            var translated = ParseLLMTranslationResponse(response.Content);
            if (translated == null)
                return null;

            foreach (var key in GermanSegments.Keys)
            {
                if (!translated.ContainsKey(key))
                {
                    _logger.LogWarning("LLM translation missing key {Key} for {Language}, using German fallback", key, targetLang);
                    translated[key] = GermanSegments[key];
                }
            }

            _logger.LogInformation("Translated prompt segments to {Language} via LLM ({Model})", targetLang, model.ModelId);
            return translated;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM translation failed for {Language}", targetLang);
            return null;
        }
    }

    private Dictionary<string, string>? ParseLLMTranslationResponse(string content)
    {
        var trimmed = content.Trim();

        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
                trimmed = trimmed[(firstNewline + 1)..];

            var lastFence = trimmed.LastIndexOf("```");
            if (lastFence > 0)
                trimmed = trimmed[..lastFence];

            trimmed = trimmed.Trim();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(trimmed);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM translation response as JSON");
            return null;
        }
    }
}
