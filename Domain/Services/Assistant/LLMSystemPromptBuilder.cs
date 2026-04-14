// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMSystemPromptBuilder
{
    private readonly IPromptTranslationProvider _translationProvider;

    private const string NavigationResponseGuide = """

NAVIGATION RESPONSE GUIDE:
- Speak in first person as Klacksy.
- Confirm the destination by name, keep 1-2 short sentences.
- On failure: be honest (permission / not loaded / renamed).
- Never use passive voice.
- Respond in the user's locale.
""";

    public LLMSystemPromptBuilder(IPromptTranslationProvider translationProvider)
    {
        _translationProvider = translationProvider;
    }

    public async Task<string> BuildSystemPromptAsync(LLMContext context, string? soulAndMemoryPrompt = null)
    {
        var language = context.Language ?? "en";
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

        var sb = new StringBuilder();
        sb.Append($@"{identitySection}{t["Intro"]}

{t["ToolUsageRules"]}

{t["HeaderUserContext"]}:
- {t["LabelUserId"]}: {context.UserId}
- {t["LabelPermissions"]}: {string.Join(", ", context.UserRights)}{settingsNote}");

        if (HasNavigateToSkill(context))
        {
            sb.Append(NavigationResponseGuide);
        }

        return sb.ToString();
    }

    private static bool HasNavigateToSkill(LLMContext context)
    {
        return context.AvailableFunctions.Any(f =>
            string.Equals(f.Name, SkillNames.NavigateTo, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasPermission(LLMContext context, string permission)
    {
        return context.UserRights.Contains(permission) || context.UserRights.Contains(Roles.Admin);
    }
}
