using Klacks.Api.Application.DTOs.LLM;
using Klacks.Api.Domain.Models.AI;

namespace Klacks.Api.Domain.Services.LLM;

public class LLMSystemPromptBuilder
{
    private const int MaxMemories = 20;

    public string BuildSystemPrompt(LLMContext context, string? soul = null, IReadOnlyList<AiMemory>? memories = null, string? guidelines = null)
    {
        var language = context.Language ?? "de";

        return language switch
        {
            "en" => BuildEnglishPrompt(context, soul, memories, guidelines),
            "fr" => BuildFrenchPrompt(context, soul, memories, guidelines),
            "it" => BuildItalianPrompt(context, soul, memories, guidelines),
            _ => BuildGermanPrompt(context, soul, memories, guidelines)
        };
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

    private static readonly string DefaultGuidelinesFallback =
        "- Be polite and professional\n" +
        "- Use available functions when users ask for them\n" +
        "- Give clear and precise instructions\n" +
        "- Always check permissions before executing functions\n" +
        "- For missing permissions: explain that the user needs to contact an administrator\n" +
        "- MANDATORY: Every address MUST be validated via validate_address before saving, regardless of the component (owner address, employee, branch, etc.). Never save an unvalidated address.\n" +
        "- If address validation fails or returns a non-exact match, NEVER offer to save the incorrect address. Instead, inform the user that the address is invalid and ask them to provide a corrected address. Do not present 'save anyway' as an option.";

    private string BuildGermanPrompt(LLMContext context, string? soul, IReadOnlyList<AiMemory>? memories, string? guidelines)
    {
        var canViewSettings = HasPermission(context, "CanViewSettings");
        var canEditSettings = HasPermission(context, "CanEditSettings");

        var settingsNote = !canViewSettings && !canEditSettings
            ? "\n- WICHTIG: Dieser Benutzer hat KEINE Berechtigung für Einstellungen. Wenn er nach Einstellungen fragt, erkläre freundlich, dass er keine Berechtigung hat und sich an einen Administrator wenden muss."
            : canViewSettings && !canEditSettings
                ? "\n- Dieser Benutzer kann Einstellungen einsehen, aber nicht ändern."
                : "";

        var soulSection = BuildSoulSection(soul);
        var memorySection = BuildMemorySection(memories, "Persistentes Wissen");
        var guidelinesSection = BuildGuidelinesSection(guidelines, "Richtlinien", DefaultGuidelinesFallback);

        return $@"{soulSection}Du bist ein hilfreicher KI-Assistent für dieses Planungs-System.
Antworte immer in der Sprache des Benutzers.
Du kannst auch allgemeine Wissensfragen beantworten, nicht nur Fragen zum System.

WICHTIGE REGELN FÜR TOOL-VERWENDUNG:
- Wenn der Benutzer eine Aktion anfordert (erstellen, löschen, navigieren, anzeigen etc.), verwende IMMER die entsprechende Funktion als Tool-Call.
- Beschreibe die Aktion nicht nur in Text – führe sie aus.
- Bei Lösch-Anfragen: Rufe zuerst die passende list_* Funktion auf um die ID zu finden, dann verwende die delete_* Funktion mit der gefundenen ID. Führe BEIDE Schritte in einer Anfrage durch.
- Bei Navigation: Verwende IMMER die navigate_to_page Funktion.
- Gib NIEMALS eine Textantwort zurück wenn eine passende Funktion verfügbar ist.

Benutzer-Kontext:
- User ID: {context.UserId}
- Berechtigungen: {string.Join(", ", context.UserRights)}{settingsNote}

Verfügbare Funktionen:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}{guidelinesSection}{memorySection}";
    }

    private string BuildEnglishPrompt(LLMContext context, string? soul, IReadOnlyList<AiMemory>? memories, string? guidelines)
    {
        var canViewSettings = HasPermission(context, "CanViewSettings");
        var canEditSettings = HasPermission(context, "CanEditSettings");

        var settingsNote = !canViewSettings && !canEditSettings
            ? "\n- IMPORTANT: This user does NOT have permission for settings. If they ask about settings, politely explain they need to contact an administrator."
            : canViewSettings && !canEditSettings
                ? "\n- This user can view settings but cannot modify them."
                : "";

        var soulSection = BuildSoulSection(soul);
        var memorySection = BuildMemorySection(memories, "Persistent Knowledge");
        var guidelinesSection = BuildGuidelinesSection(guidelines, "Guidelines", DefaultGuidelinesFallback);

        return $@"{soulSection}You are a helpful AI assistant for this planning system.
Always respond in the user's language.
You can also answer general knowledge questions, not only questions about the system.

IMPORTANT RULES FOR TOOL USAGE:
- When the user requests an action (create, delete, navigate, display etc.), ALWAYS use the corresponding function as a tool call.
- Do not just describe the action in text – execute it.
- For delete requests: First call the matching list_* function to find the ID, then use the delete_* function with the found ID. Execute BOTH steps in one request.
- For navigation: ALWAYS use the navigate_to_page function.
- NEVER return a text response when a matching function is available.

User Context:
- User ID: {context.UserId}
- Permissions: {string.Join(", ", context.UserRights)}{settingsNote}

Available Functions:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}{guidelinesSection}{memorySection}";
    }

    private string BuildFrenchPrompt(LLMContext context, string? soul, IReadOnlyList<AiMemory>? memories, string? guidelines)
    {
        var canViewSettings = HasPermission(context, "CanViewSettings");
        var canEditSettings = HasPermission(context, "CanEditSettings");

        var settingsNote = !canViewSettings && !canEditSettings
            ? "\n- IMPORTANT: Cet utilisateur n'a PAS la permission pour les paramètres. S'il demande les paramètres, expliquez poliment qu'il doit contacter un administrateur."
            : canViewSettings && !canEditSettings
                ? "\n- Cet utilisateur peut consulter les paramètres mais ne peut pas les modifier."
                : "";

        var soulSection = BuildSoulSection(soul);
        var memorySection = BuildMemorySection(memories, "Connaissances persistantes");
        var guidelinesSection = BuildGuidelinesSection(guidelines, "Directives", DefaultGuidelinesFallback);

        return $@"{soulSection}Vous êtes un assistant IA utile pour ce système de planification.
Répondez toujours dans la langue de l'utilisateur.

Contexte utilisateur:
- ID utilisateur: {context.UserId}
- Autorisations: {string.Join(", ", context.UserRights)}{settingsNote}

Fonctions disponibles:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}{guidelinesSection}{memorySection}";
    }

    private string BuildItalianPrompt(LLMContext context, string? soul, IReadOnlyList<AiMemory>? memories, string? guidelines)
    {
        var canViewSettings = HasPermission(context, "CanViewSettings");
        var canEditSettings = HasPermission(context, "CanEditSettings");

        var settingsNote = !canViewSettings && !canEditSettings
            ? "\n- IMPORTANTE: Questo utente NON ha il permesso per le impostazioni. Se chiede delle impostazioni, spiega gentilmente che deve contattare un amministratore."
            : canViewSettings && !canEditSettings
                ? "\n- Questo utente può visualizzare le impostazioni ma non può modificarle."
                : "";

        var soulSection = BuildSoulSection(soul);
        var memorySection = BuildMemorySection(memories, "Conoscenze persistenti");
        var guidelinesSection = BuildGuidelinesSection(guidelines, "Linee guida", DefaultGuidelinesFallback);

        return $@"{soulSection}Sei un assistente AI utile per questo sistema di pianificazione.
Rispondi sempre nella lingua dell'utente.

Contesto utente:
- ID utente: {context.UserId}
- Autorizzazioni: {string.Join(", ", context.UserRights)}{settingsNote}

Funzioni disponibili:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}{guidelinesSection}{memorySection}";
    }
}
