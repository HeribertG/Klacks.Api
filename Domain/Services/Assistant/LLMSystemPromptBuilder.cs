using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMSystemPromptBuilder
{
    private const int MaxMemories = 20;

    private readonly IPromptTranslationProvider _translationProvider;

    public LLMSystemPromptBuilder(IPromptTranslationProvider translationProvider)
    {
        _translationProvider = translationProvider;
    }

    public async Task<string> BuildSystemPromptAsync(LLMContext context, string? soul = null, IReadOnlyList<AiMemory>? memories = null, string? guidelines = null)
    {
        var language = context.Language ?? "de";
        var t = await _translationProvider.GetTranslationsAsync(language);

        var canViewSettings = HasPermission(context, "CanViewSettings");
        var canEditSettings = HasPermission(context, "CanEditSettings");

        var settingsNote = !canViewSettings && !canEditSettings
            ? $"\n- {t["SettingsNoPermission"]}"
            : canViewSettings && !canEditSettings
                ? $"\n- {t["SettingsViewOnly"]}"
                : "";

        var soulSection = BuildSoulSection(soul);
        var memorySection = BuildMemorySection(memories, t["HeaderPersistentKnowledge"]);
        var guidelinesSection = BuildGuidelinesSection(guidelines, t["HeaderGuidelines"], t["DefaultGuidelinesFallback"]);

        return $@"{soulSection}{t["Intro"]}

{t["ToolUsageRules"]}

{t["HeaderUserContext"]}:
- {t["LabelUserId"]}: {context.UserId}
- {t["LabelPermissions"]}: {string.Join(", ", context.UserRights)}{settingsNote}

{t["HeaderAvailableFunctions"]}:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}{guidelinesSection}{memorySection}";
    }

    private static bool HasPermission(LLMContext context, string permission)
    {
        return context.UserRights.Contains(permission) || context.UserRights.Contains("Admin");
    }

    private static string BuildSoulSection(string? soul)
    {
        if (string.IsNullOrWhiteSpace(soul))
            return string.Empty;

        return $@"
=== IDENTITY ===
{soul.Trim()}
================

";
    }

    private static string BuildGuidelinesSection(string? guidelines, string header, string fallback)
    {
        var content = string.IsNullOrWhiteSpace(guidelines) ? fallback : guidelines.Trim();

        return $@"
{header}:
{content}";
    }

    private static string BuildMemorySection(IReadOnlyList<AiMemory>? memories, string header)
    {
        if (memories == null || memories.Count == 0)
            return string.Empty;

        var topMemories = memories
            .OrderByDescending(m => m.Importance)
            .Take(MaxMemories)
            .ToList();

        var memoryLines = topMemories
            .Select(m => $"- [{m.Category}] {m.Key}: {m.Content}");

        return $@"

{header}:
{string.Join("\n", memoryLines)}";
    }
}
