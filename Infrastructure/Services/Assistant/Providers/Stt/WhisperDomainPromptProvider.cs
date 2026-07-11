// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Provides a short Klacks domain glossary per Whisper language code, used as the OpenAI-compatible
/// "prompt" field to bias Whisper transcription towards application terminology. BuildPrompt appends
/// user-maintained transcription dictionary terms to the static glossary within the Whisper prompt budget.
/// </summary>
/// <param name="whisperLanguage">Resolved Whisper language code (e.g. "de"); unknown codes fall back to the English glossary</param>
/// <param name="additionalTerms">Correct terms from the transcription dictionary appended after the glossary</param>
namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Stt;

using System.Text;

public static class WhisperDomainPromptProvider
{
    private const string FallbackLanguage = "en";
    private const int MaxAdditionalTerms = 24;
    private const int MaxPromptLength = 700;

    private static readonly Dictionary<string, string> Prompts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["de"] = "Klacks Dienstplanung: Dashboard, Dienste, Schichten, Gruppen, Mitarbeiter, Kunden, Absenzen, Ferien, Makros, versiegeln, Bestellungen, Einsatzplan, Klacksy.",
        ["en"] = "Klacks workforce scheduling: dashboard, duties, shifts, groups, employees, clients, absences, holidays, macros, seal, orders, schedule, Klacksy.",
        ["fr"] = "Klacks planification des services : tableau de bord, services, équipes, groupes, employés, clients, absences, vacances, macros, sceller, commandes, planning, Klacksy.",
        ["it"] = "Klacks pianificazione dei servizi: dashboard, servizi, turni, gruppi, dipendenti, clienti, assenze, ferie, macro, sigillare, ordini, piano di lavoro, Klacksy.",
    };

    public static string GetPrompt(string? whisperLanguage)
    {
        if (!string.IsNullOrWhiteSpace(whisperLanguage)
            && Prompts.TryGetValue(whisperLanguage.Trim(), out var prompt))
        {
            return prompt;
        }

        return Prompts[FallbackLanguage];
    }

    public static string BuildPrompt(string? whisperLanguage, IReadOnlyList<string> additionalTerms)
    {
        var basePrompt = GetPrompt(whisperLanguage);
        if (additionalTerms.Count == 0)
        {
            return basePrompt;
        }

        var extras = additionalTerms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Where(t => !basePrompt.Contains(t, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxAdditionalTerms)
            .ToList();

        if (extras.Count == 0)
        {
            return basePrompt;
        }

        var sb = new StringBuilder(basePrompt);
        var appended = 0;
        foreach (var term in extras)
        {
            var separator = appended == 0 ? " " : ", ";
            if (sb.Length + separator.Length + term.Length + 1 > MaxPromptLength)
            {
                break;
            }

            sb.Append(separator).Append(term);
            appended++;
        }

        if (appended > 0)
        {
            sb.Append('.');
        }

        return sb.ToString();
    }
}
