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

        // Klacksy explaining itself. Only unambiguous single words: "modell" or "lernen" would
        // fire on data models and machine learning talk that has nothing to do with the setup.
        ("autonomie", "explain_klacksy_autonomy"),
        ("autonomy", "explain_klacksy_autonomy"),
        ("gedächtnis", "explain_klacksy_memory"),
        ("gedaechtnis", "explain_klacksy_memory"),
        ("persönlichkeit", "explain_klacksy_personality"),
        ("persoenlichkeit", "explain_klacksy_personality"),
        ("sprachmodell", "explain_klacksy_llm_setup"),
        ("language model", "explain_klacksy_llm_setup"),

        ("rundgang", "explain_guided_tour"),
        ("walkthrough", "explain_guided_tour"),
        ("berechtigung", "explain_roles_and_permissions"),
        ("supervisor", "explain_roles_and_permissions"),
        ("sichtbarkeitsbereich", "explain_group_model"),
        ("absenzart", "explain_absence_model"),
        ("absenztyp", "explain_absence_model"),
        ("saldo", "explain_period_closing_model"),
        ("tagessperre", "explain_period_closing_model"),
        ("mitgliedschaft", "explain_address_management"),
        ("imap", "explain_email_setup"),
        ("smtp", "explain_email_setup"),

        ("firmenregel", "explain_company_rules"),
        ("company rule", "explain_company_rules"),
        ("websuche", "explain_system_context"),
        ("web search", "explain_system_context"),
        ("qualifikation", "explain_qualifications"),
        ("qualification", "explain_qualifications"),
        ("sollstunden", "explain_contracts"),
        ("vertragsvorlage", "explain_contracts"),

        // Settings concepts. Distinctive words only: "regel" or "format" alone would drag four
        // unrelated explain skills into the tool list at once, since matching is substring-based.
        ("ruhezeit", "explain_scheduling_rules_model"),
        ("höchstarbeitszeit", "explain_scheduling_rules_model"),
        ("hoechstarbeitszeit", "explain_scheduling_rules_model"),
        ("planungsregel", "explain_scheduling_rules_model"),
        ("zählerregel", "explain_compliance_rules_model"),
        ("zaehlerregel", "explain_compliance_rules_model"),
        ("gleitfenster", "explain_compliance_rules_model"),
        ("ersatzruhe", "explain_compliance_rules_model"),
        ("sperrzeit", "explain_compliance_rules_model"),
        ("feiertag", "explain_calendar_model"),
        ("kalenderauswahl", "explain_calendar_model"),
        ("ostern", "explain_calendar_model"),
        ("branche", "explain_active_industries"),
        ("überstund", "explain_overtime_model"),
        ("ueberstund", "explain_overtime_model"),
        ("overtime", "explain_overtime_model"),
        ("ldap", "explain_identity_providers"),
        ("oauth", "explain_identity_providers"),
        ("single sign", "explain_identity_providers"),
        ("individuelle periode", "explain_individual_periods"),
        ("exportformat", "explain_export_formats_model"),
        ("export-format", "explain_export_formats_model"),
        ("datev", "explain_export_formats_model"),
        ("reportvorlage", "explain_report_designer"),
        ("druckvorlage", "explain_report_designer"),
        ("report designer", "explain_report_designer"),
        ("spam", "explain_spam_rules"),
        ("zuschlagsmodus", "explain_surcharge_mode"),
        ("kumulierung", "explain_surcharge_mode"),
        ("wartungsfenster", "explain_software_updates"),
        ("rollback", "explain_software_updates"),
        ("software-update", "explain_software_updates"),
        ("sprachausgabe", "explain_klacksy_speech_setup"),
        ("spracheingabe", "explain_klacksy_speech_setup"),
        ("barge-in", "explain_klacksy_speech_setup"),
        ("diktier", "explain_klacksy_speech_setup"),
        ("whisper", "explain_whisper_setup"),
        ("skill-beziehung", "explain_klacksy_skill_relations"),
        ("konfidenz", "explain_klacksy_skill_relations"),

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
