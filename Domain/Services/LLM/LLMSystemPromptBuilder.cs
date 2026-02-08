using Klacks.Api.Application.DTOs.LLM;
using Klacks.Api.Domain.Models.AI;

namespace Klacks.Api.Domain.Services.LLM;

public class LLMSystemPromptBuilder
{
    private const int MaxMemories = 20;

    public string BuildSystemPrompt(LLMContext context, string? soul = null, IReadOnlyList<AiMemory>? memories = null)
    {
        var language = context.Language ?? "de";

        return language switch
        {
            "en" => BuildEnglishPrompt(context, soul, memories),
            "fr" => BuildFrenchPrompt(context, soul, memories),
            "it" => BuildItalianPrompt(context, soul, memories),
            _ => BuildGermanPrompt(context, soul, memories)
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

    private string BuildGermanPrompt(LLMContext context, string? soul, IReadOnlyList<AiMemory>? memories)
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

        return $@"{soulSection}Du bist ein hilfreicher KI-Assistent für dieses Planungs-System.
Antworte immer in der Sprache des Benutzers.

Benutzer-Kontext:
- User ID: {context.UserId}
- Berechtigungen: {string.Join(", ", context.UserRights)}{settingsNote}

Verfügbare Funktionen:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Richtlinien:
- Sei höflich und professionell
- Verwende die verfügbaren Funktionen, wenn der Benutzer danach fragt
- Gib klare und präzise Anweisungen
- Prüfe immer die Berechtigungen bevor du Funktionen ausführst
- Bei fehlenden Berechtigungen: erkläre dem Benutzer, dass er sich an einen Administrator wenden muss{memorySection}";
    }

    private string BuildEnglishPrompt(LLMContext context, string? soul, IReadOnlyList<AiMemory>? memories)
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

        return $@"{soulSection}You are a helpful AI assistant for this planning system.
Always respond in the user's language.

User Context:
- User ID: {context.UserId}
- Permissions: {string.Join(", ", context.UserRights)}{settingsNote}

Available Functions:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Guidelines:
- Be polite and professional
- Use available functions when users ask for them
- Give clear and precise instructions
- Always check permissions before executing functions
- For missing permissions: explain that the user needs to contact an administrator{memorySection}";
    }

    private string BuildFrenchPrompt(LLMContext context, string? soul, IReadOnlyList<AiMemory>? memories)
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

        return $@"{soulSection}Vous êtes un assistant IA utile pour ce système de planification.
Répondez toujours dans la langue de l'utilisateur.

Contexte utilisateur:
- ID utilisateur: {context.UserId}
- Autorisations: {string.Join(", ", context.UserRights)}{settingsNote}

Fonctions disponibles:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Directives:
- Soyez poli et professionnel
- Utilisez les fonctions disponibles lorsque les utilisateurs le demandent
- Donnez des instructions claires et précises
- Vérifiez toujours les autorisations avant d'exécuter des fonctions
- En cas d'autorisations manquantes: expliquez que l'utilisateur doit contacter un administrateur{memorySection}";
    }

    private string BuildItalianPrompt(LLMContext context, string? soul, IReadOnlyList<AiMemory>? memories)
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

        return $@"{soulSection}Sei un assistente AI utile per questo sistema di pianificazione.
Rispondi sempre nella lingua dell'utente.

Contesto utente:
- ID utente: {context.UserId}
- Autorizzazioni: {string.Join(", ", context.UserRights)}{settingsNote}

Funzioni disponibili:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Linee guida:
- Sii educato e professionale
- Usa le funzioni disponibili quando gli utenti le richiedono
- Dai istruzioni chiare e precise
- Controlla sempre le autorizzazioni prima di eseguire funzioni
- Per autorizzazioni mancanti: spiega che l'utente deve contattare un amministratore{memorySection}";
    }
}
