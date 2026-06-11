// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class LLMSystemPromptBuilder
{
    private readonly IPromptTranslationProvider _translationProvider;

    private const string CurrentViewHeader = "=== CURRENT VIEW ===";
    private const string CurrentViewFooter = "=== END CURRENT VIEW ===";

    private const string NavigationResponseGuide = """

NAVIGATION RESPONSE GUIDE:
- Speak in first person as Klacksy.
- Confirm the destination by name, keep 1-2 short sentences.
- On failure: be honest (permission / not loaded / renamed).
- Never use passive voice.
- Respond in the user's locale.
""";

    private static readonly IReadOnlyDictionary<string, string> LanguageDirectives =
        new Dictionary<string, string>
        {
            ["de"] = "Antworte ausschließlich auf Deutsch, unabhängig davon, in welcher Sprache der Benutzer schreibt oder spricht.",
            ["en"] = "Respond exclusively in English, regardless of the language the user writes or speaks in.",
            ["fr"] = "Réponds exclusivement en français, quelle que soit la langue dans laquelle l'utilisateur écrit ou parle.",
            ["it"] = "Rispondi esclusivamente in italiano, indipendentemente dalla lingua in cui l'utente scrive o parla.",
        };

    public LLMSystemPromptBuilder(IPromptTranslationProvider translationProvider)
    {
        _translationProvider = translationProvider;
    }

    public async Task<string> BuildSystemPromptAsync(LLMContext context, string? soulAndMemoryPrompt = null)
    {
        var language = NormalizeLanguage(context.Language);
        var t = await _translationProvider.GetTranslationsAsync(language);
        var languageDirective = LanguageDirectives.GetValueOrDefault(language, LanguageDirectives["en"]);

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
        sb.Append($@"{languageDirective}

{identitySection}{t["Intro"]}

{t["ToolUsageRules"]}

{t["HeaderUserContext"]}:
- {t["LabelUserId"]}: {context.UserId}
- {t["LabelPermissions"]}: {string.Join(", ", context.UserRights)}{settingsNote}");

        var currentView = RenderCurrentViewBlock(context.PageContext);
        if (currentView != null)
        {
            sb.AppendLine();
            sb.AppendLine();
            sb.Append(currentView);
        }

        if (HasNavigateToSkill(context))
        {
            sb.Append(NavigationResponseGuide);
        }

        return sb.ToString();
    }

    private static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        var normalized = language.Trim().ToLowerInvariant();
        var separator = normalized.IndexOf('-');
        return separator > 0 ? normalized[..separator] : normalized;
    }

    private static string? RenderCurrentViewBlock(AssistantPageContext? pageContext)
    {
        if (pageContext == null || !pageContext.HasAny())
        {
            return null;
        }

        var sb = new StringBuilder();
        sb.AppendLine(CurrentViewHeader);
        if (!string.IsNullOrWhiteSpace(pageContext.CurrentRoute))
        {
            sb.AppendLine($"- route: {pageContext.CurrentRoute}");

            var pageExplainSkill = PageExplainSkillRoutes.ResolveSkillName(pageContext.CurrentRoute);
            if (pageExplainSkill != null)
            {
                sb.AppendLine(
                    $"- MANDATORY: for ANY question about this page, its elements/cards, or how to create/edit something here, " +
                    $"call {pageExplainSkill} FIRST (level=elements for element/mask/how-to questions) and answer ONLY from its result — " +
                    "never from memory or earlier turns.");
            }
        }
        if (!string.IsNullOrWhiteSpace(pageContext.SelectedGroupId))
        {
            sb.AppendLine($"- selectedGroupId: {pageContext.SelectedGroupId}");
        }
        if (!string.IsNullOrWhiteSpace(pageContext.SelectedPeriodFrom))
        {
            sb.AppendLine($"- selectedPeriodFrom: {pageContext.SelectedPeriodFrom}");
        }
        if (!string.IsNullOrWhiteSpace(pageContext.SelectedPeriodUntil))
        {
            sb.AppendLine($"- selectedPeriodUntil: {pageContext.SelectedPeriodUntil}");
        }
        if (!string.IsNullOrWhiteSpace(pageContext.SelectedClientId))
        {
            sb.AppendLine($"- selectedClientId: {pageContext.SelectedClientId}");
        }
        sb.Append(CurrentViewFooter);
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
