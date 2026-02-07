using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM;

public class LLMSystemPromptBuilder
{
    public string BuildSystemPrompt(LLMContext context)
    {
        var language = context.Language ?? "de";

        return language switch
        {
            "en" => BuildEnglishPrompt(context),
            "fr" => BuildFrenchPrompt(context),
            "it" => BuildItalianPrompt(context),
            _ => BuildGermanPrompt(context)
        };
    }

    private static bool HasPermission(LLMContext context, string permission)
    {
        return context.UserRights.Contains(permission) || context.UserRights.Contains("Admin");
    }

    private string BuildGermanPrompt(LLMContext context)
    {
        var canViewSettings = HasPermission(context, "CanViewSettings");
        var canEditSettings = HasPermission(context, "CanEditSettings");

        var settingsNote = !canViewSettings && !canEditSettings
            ? "\n- WICHTIG: Dieser Benutzer hat KEINE Berechtigung für Einstellungen. Wenn er nach Einstellungen fragt, erkläre freundlich, dass er keine Berechtigung hat und sich an einen Administrator wenden muss."
            : canViewSettings && !canEditSettings
                ? "\n- Dieser Benutzer kann Einstellungen einsehen, aber nicht ändern."
                : "";

        return $@"Du bist ein hilfreicher KI-Assistent für dieses Planungs-System.
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
- Bei fehlenden Berechtigungen: erkläre dem Benutzer, dass er sich an einen Administrator wenden muss";
    }

    private string BuildEnglishPrompt(LLMContext context)
    {
        var canViewSettings = HasPermission(context, "CanViewSettings");
        var canEditSettings = HasPermission(context, "CanEditSettings");

        var settingsNote = !canViewSettings && !canEditSettings
            ? "\n- IMPORTANT: This user does NOT have permission for settings. If they ask about settings, politely explain they need to contact an administrator."
            : canViewSettings && !canEditSettings
                ? "\n- This user can view settings but cannot modify them."
                : "";

        return $@"You are a helpful AI assistant for this planning system.
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
- For missing permissions: explain that the user needs to contact an administrator";
    }

    private string BuildFrenchPrompt(LLMContext context)
    {
        var canViewSettings = HasPermission(context, "CanViewSettings");
        var canEditSettings = HasPermission(context, "CanEditSettings");

        var settingsNote = !canViewSettings && !canEditSettings
            ? "\n- IMPORTANT: Cet utilisateur n'a PAS la permission pour les paramètres. S'il demande les paramètres, expliquez poliment qu'il doit contacter un administrateur."
            : canViewSettings && !canEditSettings
                ? "\n- Cet utilisateur peut consulter les paramètres mais ne peut pas les modifier."
                : "";

        return $@"Vous êtes un assistant IA utile pour ce système de planification.
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
- En cas d'autorisations manquantes: expliquez que l'utilisateur doit contacter un administrateur";
    }

    private string BuildItalianPrompt(LLMContext context)
    {
        var canViewSettings = HasPermission(context, "CanViewSettings");
        var canEditSettings = HasPermission(context, "CanEditSettings");

        var settingsNote = !canViewSettings && !canEditSettings
            ? "\n- IMPORTANTE: Questo utente NON ha il permesso per le impostazioni. Se chiede delle impostazioni, spiega gentilmente che deve contattare un amministratore."
            : canViewSettings && !canEditSettings
                ? "\n- Questo utente può visualizzare le impostazioni ma non può modificarle."
                : "";

        return $@"Sei un assistente AI utile per questo sistema di pianificazione.
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
- Per autorizzazioni mancanti: spiega che l'utente deve contattare un amministratore";
    }
}
