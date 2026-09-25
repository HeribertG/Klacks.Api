// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The short sentences EscalationNotifier sends outside the wake-up path: per chain purpose a
/// confirmation back to whoever just acknowledged a chain and a quiet note to the stages that were
/// notified before them, plus - for the approval chain only - the note that closes a window nobody used.
/// The absence pair speaks of a shift being covered, the approval texts of a remediation being released
/// or not - both with the responder's name so nobody who was asked earlier acts a
/// second time. Deliberately separate from MessengerProactiveTexts, whose own test enforces a strict 1:1
/// with MessengerWakeUpPolicy - none of these messages is a wake-up alert, so they do not belong in that
/// bijection.
/// The four core languages live in the table below, the 21 plugin languages are merged in at startup from
/// each pack's assistant-texts.json by AssistantTextsPluginLoader (the key IS the pack key). Resolution is
/// LocalizedTextCatalogue's: English for a tag that no core table and no loaded pack claims; a language
/// whose loaded pack lacks a key resolves to nothing (EscalationHandoffTextService then uses English and
/// logs a warning) - a gap the catalogue guard keeps from shipping. Placeholders are {{name}}, filled by
/// DoubleBraceTemplate in a single pass.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Constants;

public static class EscalationHandoffTexts
{
    private const string German = "de";
    private const string English = "en";
    private const string French = "fr";
    private const string Italian = "it";

    private const string Prefix = "assistant.escalationHandoff.";

    public const string AcknowledgedConfirmation = Prefix + "acknowledgedConfirmation";
    public const string HandoffQuietNote = Prefix + "handoffQuietNote";
    public const string ApprovalAcknowledgedConfirmation = Prefix + "approvalAcknowledgedConfirmation";
    public const string ApprovalHandoffQuietNote = Prefix + "approvalHandoffQuietNote";
    public const string ApprovalExhaustedNote = Prefix + "approvalExhaustedNote";

    /// <summary>Every key a language pack has to ship in assistant-texts.json. The catalogue guard reads exactly this list.</summary>
    public static readonly IReadOnlyList<string> RequiredKeys =
    [
        AcknowledgedConfirmation, HandoffQuietNote, ApprovalAcknowledgedConfirmation, ApprovalHandoffQuietNote,
        ApprovalExhaustedNote
    ];

    /// <summary>
    /// The languages whose texts are authored in code rather than shipped by a pack: MultiLanguage.CoreLanguages,
    /// so a fifth core language cannot be added without the catalogue guard going red.
    /// </summary>
    public static readonly IReadOnlyList<string> CoreLanguages = MultiLanguage.CoreLanguages;

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CoreTexts =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            [AcknowledgedConfirmation] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Danke, du übernimmst den Dienst am {{date}} von {{employee}}.",
                [English] = "Thanks, you're now covering the {{date}} shift for {{employee}}.",
                [French] = "Merci, tu reprends le service du {{date}} pour {{employee}}.",
                [Italian] = "Grazie, ora copri il turno del {{date}} per {{employee}}."
            },
            [HandoffQuietNote] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "{{responder}} hat den Dienst am {{date}} von {{employee}} übernommen.",
                [English] = "{{responder}} has taken over the {{date}} shift for {{employee}}.",
                [French] = "{{responder}} a repris le service du {{date}} pour {{employee}}.",
                [Italian] = "{{responder}} ha rilevato il turno del {{date}} per {{employee}}."
            },
            [ApprovalAcknowledgedConfirmation] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Danke, du hast die Massnahme {{action}} zur Feststellung {{finding}} freigegeben. Klacksy führt sie in Kürze unter deinen Rechten aus und berichtet dir das Ergebnis.",
                [English] = "Thanks, you approved the action {{action}} for the finding {{finding}}. Klacksy will carry it out shortly under your rights and report the result to you.",
                [French] = "Merci, tu as approuvé l'action {{action}} pour le constat {{finding}}. Klacksy va l'exécuter sous peu avec tes droits et te communiquer le résultat.",
                [Italian] = "Grazie, hai approvato l'azione {{action}} per la rilevazione {{finding}}. Klacksy la eseguirà a breve con i tuoi diritti e ti comunicherà il risultato."
            },
            [ApprovalHandoffQuietNote] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "{{responder}} hat die Massnahme {{action}} zur Feststellung {{finding}} freigegeben; du musst nichts mehr tun.",
                [English] = "{{responder}} has approved the action {{action}} for the finding {{finding}}; nothing is left for you to do.",
                [French] = "{{responder}} a approuvé l'action {{action}} pour le constat {{finding}} ; tu n'as plus rien à faire.",
                [Italian] = "{{responder}} ha approvato l'azione {{action}} per la rilevazione {{finding}}; non devi fare altro."
            },
            [ApprovalExhaustedNote] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Niemand hat die Massnahme {{action}} zur Feststellung {{finding}} freigegeben; die Frist ist abgelaufen. Klacksy fragt am nächsten Firmentag erneut. Soll sie heute noch laufen, delegiere die Feststellung direkt.",
                [English] = "Nobody approved the action {{action}} for the finding {{finding}}; the window has lapsed. Klacksy will ask again on the next company day. To still have it run today, delegate the finding directly.",
                [French] = "Personne n'a approuvé l'action {{action}} pour le constat {{finding}} ; le délai est écoulé. Klacksy redemandera le prochain jour ouvré. Pour qu'elle s'exécute encore aujourd'hui, délègue directement le constat.",
                [Italian] = "Nessuno ha approvato l'azione {{action}} per la rilevazione {{finding}}; il termine è scaduto. Klacksy lo richiederà il prossimo giorno aziendale. Se deve essere eseguita ancora oggi, delega direttamente la rilevazione."
            }
        };

    private static readonly LocalizedTextCatalogue Catalogue = new(CoreTexts);

    /// <summary>
    /// Adds (or replaces) the texts of one language pack. Called once per pack at startup by
    /// AssistantTextsPluginLoader.
    /// </summary>
    /// <param name="languageCode">Locale of the pack the texts were read from</param>
    /// <param name="texts">The pack's texts, keyed by the keys of RequiredKeys</param>
    public static void Configure(string languageCode, IReadOnlyDictionary<string, string> texts) =>
        Catalogue.Configure(languageCode, texts);

    /// <summary>
    /// Resolves one text for a language. False only when a loaded pack lacks the key; an unknown language
    /// (including one whose pack was never loaded) resolves to English.
    /// </summary>
    /// <param name="key">One of RequiredKeys</param>
    /// <param name="language">Installation language, possibly regional (de-CH), or null</param>
    /// <param name="text">The resolved template, empty when nothing resolves</param>
    public static bool TryGetText(string key, string? language, out string text) =>
        Catalogue.TryGetText(key, language, out text);

    /// <summary>The English text of a key: the floor when a loaded pack lacks a key.</summary>
    /// <param name="key">One of RequiredKeys</param>
    public static string EnglishOf(string key) => CoreTexts[key][LanguageConfig.DefaultLanguageFallback];

    /// <summary>Every key of the core catalogue, for the guard tests.</summary>
    internal static IReadOnlyCollection<string> Keys => Catalogue.Keys;

    /// <summary>The per-language core variants of one key, for the guard tests.</summary>
    /// <param name="key">One of RequiredKeys</param>
    internal static IReadOnlyDictionary<string, string> VariantsOf(string key) => Catalogue.VariantsOf(key);

    /// <summary>Discards every configured pack. Test-only, see LocalizedTextCatalogue.Reset.</summary>
    internal static void Reset() => Catalogue.Reset();
}
