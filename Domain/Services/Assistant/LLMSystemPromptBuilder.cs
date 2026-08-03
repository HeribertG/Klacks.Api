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

    private const string ToolCallBatchingGuide = """

TOOL CALL BATCHING:
- When a task needs multiple INDEPENDENT tool calls, emit them all together in ONE response
  instead of one call per turn — each extra turn costs the user a full round-trip of waiting.
- Only sequence tool calls when a later call genuinely needs the result of an earlier one
  (e.g. look up an id first, then use it).
""";

    private const string InternalDisclosureGuide = """

INTERNAL DISCLOSURE (mandatory, applies to every answer):
- Never narrate internal steps, retries, or corrections (e.g. "let me resolve the correct id first",
  "I need to fix that internally") — the user does not know or care about internal ids, tool calls, or
  retries. If an internal attempt fails and you retry, do it silently and only show the final result.
- Never expose internal identifiers to the user: enum values (e.g. status names like OriginalShift),
  GUIDs, skill names, tool names, or knowledge file names. Translate them into plain business language
  instead (e.g. "this shift comes from a sealed order, so these fields are read-only").
- Never expose internal page, navigation or scroll-target keys (e.g. "reports", "report-defaults",
  the `page`/`target` values you pass to navigate_to) to the user, not even in parentheses next to a
  translated label. These are targeting identifiers for your own tool calls, not display names — refer
  to the destination only by its plain, translated name.
- This applies EVEN when no tool for the task is in your current tool set. Say plainly, in one plain
  business sentence, that you cannot do this right now — never name the missing tool/skill/function
  (e.g. not "the function apply_grouping is not available"), never list what you could do
  instead by naming other tools. The user cannot act on an internal name; it only confuses them.
""";

    private const string NavigationResponseGuide = """

NAVIGATION RESPONSE GUIDE:
- Speak in first person as Klacksy.
- Confirm the destination by name, keep 1-2 short sentences.
- On failure: be honest (permission / not loaded / renamed).
- Never use passive voice.
- Respond in the user's locale.
- Never mention or compare fields you only saw in a tool result if they don't help the user tell two
  candidates apart (e.g. an empty company field on an employee) — that only confuses them. Use only
  clearly distinguishing details like name and the id number shown to the user.
""";

    private const string HonestyAndToolCallGuide = """

TOOL CALLS & HONESTY (mandatory):
- Never write <function_calls>, <invoke>, or any tool-call/XML markup as literal text. Tool calls happen
  only through the native tool mechanism; text that looks like a tool call does nothing at all.
- Never invent a tool or skill name. Use only the tools actually offered to you. If none of them can do
  what the user asked, say plainly that you cannot do it — do not pretend.
- Every tool runs synchronously and finishes before you answer. Never say an action is "running in the
  background", "in progress", or that you will "report back when it is done" — there is no such thing.
- Only claim that something was created, changed, assigned, grouped or deleted if a tool actually
  returned a successful result in THIS turn. If you called no tool, nothing happened — never assert
  otherwise.
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
        if (context.IsNonConversational)
        {
            return string.Empty;
        }

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

        if (context.AvailableFunctions.Count > 0)
        {
            sb.Append(ToolCallBatchingGuide);
        }

        sb.Append(HonestyAndToolCallGuide);
        sb.Append(InternalDisclosureGuide);

        if (HasNavigateToSkill(context))
        {
            sb.Append(NavigationResponseGuide);
        }

        if (context.IsVoiceMode)
        {
            sb.Append(VoiceModeInstructionConstants.SpokenAnswerDirective);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Renders the per-turn/per-navigation blocks (current page/route, entity grounding) that must
    /// live in the volatile system-prompt segment instead of the stable one, since either can change
    /// between two turns of the same conversation (P1b of the Klacksy memory redesign). Returns null
    /// when neither block applies to this turn.
    /// </summary>
    /// <param name="context">Turn context providing PageContext and EntityGroundingBlock</param>
    public static string? BuildVolatileAdditions(LLMContext context)
    {
        var sb = new StringBuilder();

        var currentView = RenderCurrentViewBlock(context.PageContext);
        if (currentView != null)
        {
            sb.Append(currentView);
        }

        if (!string.IsNullOrWhiteSpace(context.EntityGroundingBlock))
        {
            if (sb.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
            }

            sb.Append(context.EntityGroundingBlock.Trim());
        }

        return sb.Length > 0 ? sb.ToString() : null;
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
