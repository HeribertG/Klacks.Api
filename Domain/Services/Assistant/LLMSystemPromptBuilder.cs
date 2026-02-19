using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMSystemPromptBuilder
{
    private readonly IPromptTranslationProvider _translationProvider;

    public LLMSystemPromptBuilder(IPromptTranslationProvider translationProvider)
    {
        _translationProvider = translationProvider;
    }

    public async Task<string> BuildSystemPromptAsync(LLMContext context, string? soulAndMemoryPrompt = null)
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

        var identitySection = !string.IsNullOrWhiteSpace(soulAndMemoryPrompt)
            ? soulAndMemoryPrompt.Trim() + "\n\n"
            : "";

        return $@"{identitySection}{t["Intro"]}

{t["ToolUsageRules"]}

{t["HeaderUserContext"]}:
- {t["LabelUserId"]}: {context.UserId}
- {t["LabelPermissions"]}: {string.Join(", ", context.UserRights)}{settingsNote}

{t["HeaderAvailableFunctions"]}:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}";
    }

    private static bool HasPermission(LLMContext context, string permission)
    {
        return context.UserRights.Contains(permission) || context.UserRights.Contains("Admin");
    }
}
