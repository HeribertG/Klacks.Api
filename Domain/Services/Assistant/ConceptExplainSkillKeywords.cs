// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps high-value concept and page-name keywords in the user message to the explain skill
/// that documents them, so questions about a page or concept get their explain skill
/// guaranteed in the LLM tool list independent of vector retrieval quality — also when the
/// user asks about a DIFFERENT page than the one they are on (the route guarantee only
/// covers the current page) and despite typos (keywords are tolerant prefixes like "dashb").
/// </summary>
/// <param name="userMessage">The current user chat message; matched case-insensitively</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ConceptExplainSkillKeywords
{
    private static readonly IReadOnlyList<(string Keyword, string SkillName)> KeywordToSkill =
    [
        ("bestellung", SkillNames.ExplainShiftLifecycle),
        ("versiegel", SkillNames.ExplainShiftLifecycle),
        ("sealed order", SkillNames.ExplainShiftLifecycle),
        ("seal an order", SkillNames.ExplainShiftLifecycle),

        ("24h", "find_split_shift_candidates"),
        ("24-stunden", "find_split_shift_candidates"),
        ("24 stunden", "find_split_shift_candidates"),
        ("geteilt", "find_split_shift_candidates"),
        ("aufteil", "find_split_shift_candidates"),
        ("split", "find_split_shift_candidates"),
        ("drehplan", "find_split_shift_candidates"),
        ("rotation", "find_split_shift_candidates"),

        // The same cut/split intent must ALSO reach the action skill cut_shift, not only the
        // discovery skill above — otherwise the model lands on the cut page or writes manual
        // instructions because the one tool it needed to perform the split was never offered.
        ("24h", "cut_shift"),
        ("24-stunden", "cut_shift"),
        ("24 stunden", "cut_shift"),
        ("geteilt", "cut_shift"),
        ("aufteil", "cut_shift"),
        ("zerteil", "cut_shift"),
        ("split", "cut_shift"),
        ("schneid", "cut_shift"),
        ("zerschneid", "cut_shift"),
        ("trenn", "cut_shift"),
        ("drehplan", "cut_shift"),
        ("rotation", "cut_shift"),

        ("sporadi", "explain_shift_sporadic"),
        ("zeitfenster", "explain_shift_time_range"),
        ("zeitbereich", "explain_shift_time_range"),
        ("zeitrahmen", "explain_shift_time_range"),
        ("time range", "explain_shift_time_range"),
        ("schichtvorlage", "explain_shift_container"),
        ("container", "explain_shift_container"),
        ("makro", "explain_macro_editor"),
        ("macro", "explain_macro_editor"),
        ("planungsassistent", "explain_planning_assistant"),
        ("planungs-assistent", "explain_planning_assistant"),
        ("planning assistant", "explain_planning_assistant"),

        ("dashb", "explain_page_dashboard"),
        ("übersicht", "explain_page_dashboard"),
        ("uebersicht", "explain_page_dashboard"),
        ("overview", "explain_page_dashboard"),
        ("einsatzplan", "explain_page_schedule"),
        ("dienstplan", "explain_page_schedule"),
        ("schichtplan", "explain_page_schedule"),
        ("absenz", "explain_page_absence"),
        ("abwesenheit", "explain_page_absence"),
        ("verfügbark", "explain_page_availability"),
        ("verfuegbark", "explain_page_availability"),
        ("availability", "explain_page_availability"),
        ("dienstliste", "explain_page_shifts"),
        ("dienste-seite", "explain_page_shifts"),
        ("dienste seite", "explain_page_shifts"),
        ("dienste", "explain_page_shifts"),
        ("schichten", "explain_page_shifts"),
        ("mitarbeiterliste", "explain_page_employees"),
        ("personalliste", "explain_page_employees"),
        ("adressen", "explain_page_employees"),
        ("adressverwaltung", "explain_page_employees"),
        ("address", "explain_page_employees"),
        ("gruppenbaum", "explain_page_groups"),
        ("gruppenverwaltung", "explain_page_groups"),
        ("gruppe", "explain_page_groups"),
        ("periodenabschluss", "explain_page_period_closing"),
        ("period closing", "explain_page_period_closing"),
        ("posteingang", "explain_page_inbox"),
        ("inbox", "explain_page_inbox"),
        ("einstellung", "explain_page_settings_overview"),
        ("profil", "explain_page_profile"),
    ];

    public static IReadOnlyList<string> ResolveSkillNames(string? userMessage)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return [];
        }

        return KeywordToSkill
            .Where(m => userMessage.Contains(m.Keyword, StringComparison.OrdinalIgnoreCase))
            .Select(m => m.SkillName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
