// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Core-language (de/en/fr/it) word lists behind AffirmationDetector, split by what a word can prove. ReplyWords are
/// pure agreements that may open a confirmation; GoAheadWords are imperatives ("mach", "go", "weiter") that only count
/// inside a very short reply because the same words open ordinary requests; AmbiguousReplyWords ("si") are an agreement
/// only when set off by punctuation. CourtesyWords and FillerWords are skipped in front of the opening word, and
/// CompanionWords are the small words a short reply may carry ("mach das", "do it", "alles klar"). ConditionalWords
/// anywhere in a message turn an apparent yes into a conditional one ("ja, aber nur für Montag"). AffirmationWords is
/// the union IsAffirmation scans for.
/// </summary>

namespace Klacks.Api.Domain.Services.Assistant;

internal static class AffirmationVocabulary
{
    public static readonly HashSet<string> ReplyWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ja", "jawohl", "jep", "jo", "klar", "ok", "okay", "okey", "gerne", "genau", "passt", "stimmt", "richtig",
        "einverstanden", "sicher", "natürlich", "natuerlich",
        "yes", "yep", "yeah", "yup", "sure", "absolutely", "affirmative",
        "oui", "ouais", "daccord",
        "sì", "certo",
    };

    public static readonly HashSet<string> GoAheadWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "mach", "machen", "los", "weiter", "fortfahren", "ausführen", "ausfuehren", "bestätige", "bestaetige",
        "bestätigen", "bestaetigen", "bestätigt",
        "confirm", "confirmed", "proceed", "continue", "go", "do", "correct",
        "confirme", "confirmer", "continuer",
        "conferma", "confermare", "procedi", "vai",
    };

    public static readonly HashSet<string> AmbiguousReplyWords = new(StringComparer.OrdinalIgnoreCase) { "si" };

    public static readonly HashSet<string> CourtesyWords = new(StringComparer.OrdinalIgnoreCase) { "bitte", "please" };

    public static readonly HashSet<string> FillerWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "ähm", "äh", "ehm", "hmm", "hm", "also", "well", "euh", "uh", "uhm", "erm",
    };

    public static readonly HashSet<string> CompanionWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "das", "es", "alles", "gut", "so", "dann", "jetzt", "sofort", "doch", "mal", "schon",
        "it", "that", "this", "ahead", "on", "now", "good", "then", "for", "lets",
        "bien", "bene", "ça", "ca", "que",
    };

    public static readonly HashSet<string> NegationWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "nein", "nicht", "kein", "keine", "keinen", "stop", "stopp", "abbrechen", "abbruch", "halt", "warte",
        "no", "not", "dont", "cancel", "wait",
        "non", "pas", "annuler",
    };

    public static readonly HashSet<string> ConditionalWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "aber", "jedoch", "allerdings", "sondern", "nur", "erst", "zuerst", "ausser", "außer", "ausgenommen", "wenn",
        "falls", "sofern",
        "but", "however", "only", "first", "except", "unless", "if",
        "mais", "seulement", "sauf", "dabord", "sinon",
        "ma", "però", "solo", "soltanto", "tranne", "eccetto",
    };

    public static readonly HashSet<string> AffirmationWords = BuildAffirmationWords();

    private static HashSet<string> BuildAffirmationWords()
    {
        var union = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        union.UnionWith(ReplyWords);
        union.UnionWith(GoAheadWords);
        union.UnionWith(AmbiguousReplyWords);
        union.UnionWith(CourtesyWords);
        return union;
    }
}
