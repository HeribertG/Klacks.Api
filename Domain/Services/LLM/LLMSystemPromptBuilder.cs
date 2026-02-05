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

    private string BuildGermanPrompt(LLMContext context)
    {
        return $@"Du bist ein hilfreicher KI-Assistent für dieses Planungs-System.

Benutzer-Kontext:
- User ID: {context.UserId}
- Berechtigungen: {string.Join(", ", context.UserRights)}

Verfügbare Funktionen:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Richtlinien:
- Sei höflich und professionell
- Verwende die verfügbaren Funktionen, wenn der Benutzer danach fragt
- Gib klare und präzise Anweisungen";
    }

    private string BuildEnglishPrompt(LLMContext context)
    {
        return $@"You are a helpful AI assistant for this planning system.

User Context:
- User ID: {context.UserId}
- Permissions: {string.Join(", ", context.UserRights)}

Available Functions:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Guidelines:
- Be polite and professional
- Use available functions when users ask for them
- Give clear and precise instructions";
    }

    private string BuildFrenchPrompt(LLMContext context)
    {
        return $@"Vous êtes un assistant IA utile pour ce système de planification.

Contexte utilisateur:
- ID utilisateur: {context.UserId}
- Autorisations: {string.Join(", ", context.UserRights)}

Fonctions disponibles:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Directives:
- Soyez poli et professionnel
- Utilisez les fonctions disponibles lorsque les utilisateurs le demandent
- Donnez des instructions claires et précises";
    }

    private string BuildItalianPrompt(LLMContext context)
    {
        return $@"Sei un assistente AI utile per questo sistema di pianificazione.

Contesto utente:
- ID utente: {context.UserId}
- Autorizzazioni: {string.Join(", ", context.UserRights)}

Funzioni disponibili:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Linee guida:
- Sii educato e professionale
- Usa le funzioni disponibili quando gli utenti le richiedono
- Dai istruzioni chiare e precise";
    }
}
