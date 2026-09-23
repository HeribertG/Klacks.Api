// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Health, symptom, diagnosis and medical terms a clarification question must never contain, per
/// language (lower case, often word stems). Klacksy only asks whether and when an employee will be
/// absent, never about their health. Generic words for "sick" (krank, sick, malade, malato and their
/// equivalents) are deliberately NOT listed: "does that mean you are sick and cannot work your shift
/// today?" is exactly the question the dialog is meant to ask. The guard checks a question against all
/// languages at once. Languages without spaces between words (SubstringMatchLanguages) and languages
/// that glue nouns into closed compounds (CompoundSubstringMatchLanguages, German: Rückenschmerzen,
/// Hausarzt, Arbeitsunfall) match anywhere in the text; all others match only at the start of a word.
/// AllowedAbsencePhrases are fixed sick-leave phrasings that contain a listed term (maladie, malattia,
/// krankheit) but only state an absence; they are removed from the text before the terms are matched.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ClarificationHealthTerms
{
    public static readonly IReadOnlySet<string> SubstringMatchLanguages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ja", "th", "zh-CN", "zh-TW" };

    public static readonly IReadOnlySet<string> CompoundSubstringMatchLanguages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "de" };

    public static readonly IReadOnlyList<string> AllowedAbsencePhrases =
    [
        "arrêt maladie", "congé maladie", "en maladie", "in malattia", "krankheitsbedingt", "sick leave"
    ];

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ByLanguage =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] =
            [
                "symptom", "diagnos", "befund", "fieber", "schmerz", "arzt", "ärzt", "grippe", "corona",
                "covid", "erkält", "husten", "migräne", "kopfweh", "kopfschmerz", "übelkeit", "erbrech",
                "durchfall", "infekt", "entzündung", "verletz", "unfall", "operation", "schwanger", "psych",
                "depress", "burnout", "medikament", "krankheit", "beschwerde", "attest", "spital",
                "krankenhaus", "klinik", "therapie"
            ],
            ["en"] =
            [
                "symptom", "diagnos", "fever", "pain", "doctor", "physician", "flu", "influenza", "covid",
                "corona", "cough", "migraine", "headache", "nausea", "vomit", "diarrh", "infection",
                "inflammation", "injur", "accident", "surgery", "operation", "pregnan", "psych", "depress",
                "burnout", "medication", "medicine", "illness", "disease", "hospital", "clinic", "therapy",
                "medical"
            ],
            ["fr"] =
            [
                "symptôme", "diagnos", "fièvre", "douleur", "médecin", "docteur", "grippe", "covid",
                "corona", "toux", "migraine", "mal de tête", "nausée", "vomi", "diarrhée", "infection",
                "inflammation", "blessure", "blessé", "accident", "chirurgi", "opération", "enceinte",
                "grossesse", "psych", "dépress", "burnout", "médicament", "maladie", "hôpital", "clinique",
                "thérapie", "médical"
            ],
            ["it"] =
            [
                "sintom", "diagnos", "febbre", "dolor", "medic", "dottore", "influenza", "covid", "corona",
                "tosse", "emicrania", "mal di testa", "nausea", "vomit", "diarrea", "infezione",
                "infiammazione", "ferit", "incidente", "intervento", "operazione", "incinta", "gravidanza",
                "psic", "depress", "burnout", "farmac", "malattia", "ospedale", "clinica", "terapia"
            ]
        };
}
