// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Server-side sentences for the few proactive events that may leave Klacks over a messenger.
/// A messenger has no i18n runtime and no browser session, so the frontend catalogue that renders
/// every other proactive message cannot be reached; without this table the recipient reads the raw
/// key ("assistant.proactive.unstaffedShift") instead of a sentence. Decision E56 requires the
/// messenger chat to run in the current Klacks language.
/// Scope is deliberately minimal: only the keys MessengerWakeUpPolicy admits are listed here, so
/// this is a handful of strings and not a second translation system. A new key belongs here ONLY
/// when it is also added to that policy.
///
/// ONE SOURCE PER LANGUAGE, NO SECOND TRANSLATION. The four core languages live in the table below, as
/// verbatim copies of the same key in the frontend catalogues Klacks.Ui/src/assets/i18n/{de,en,fr,it}.json
/// (drift is caught by MessengerProactiveTextsTests.CatalogueMatchesTheFrontendTranslationFiles, which reads
/// the Ui JSON files whenever the frontend repository sits next to this one). The 21 plugin languages are
/// NOT copied: AssistantTextsPluginLoader reads the same keys, unchanged, from each pack's
/// translations.json - the very file the frontend gets - and merges them in at startup, so inbox and
/// messenger cannot tell one recipient two different sentences. Resolution is LocalizedTextCatalogue's:
/// English for a tag that no core table and no loaded pack claims; a language whose loaded pack lacks a key
/// resolves to nothing (ProactiveMessengerTextComposer then uses English and logs a warning), a gap the
/// catalogue guard keeps from shipping.
/// Affected keys: assistant.proactive.unstaffedShift, assistant.proactive.workDroppedByErpImport,
/// assistant.proactive.orderImportFailed, assistant.proactive.escalationStageAlert,
/// assistant.proactive.dailyDigest.
///
/// Placeholders use the frontend's ngx-translate form ({{name}}) so the catalogues stay literally
/// comparable; ProactiveMessengerTextComposer substitutes them from SummaryParams through
/// DoubleBraceTemplate.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Constants;

public static class MessengerProactiveTexts
{
    private const string German = "de";
    private const string English = "en";
    private const string French = "fr";
    private const string Italian = "it";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CoreTexts =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            [ProactiveMessageI18nKeys.UnstaffedShift] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Eine Schicht am {{date}} (in {{days}} Tag(en)) ist noch unbesetzt.",
                [English] = "A shift on {{date}} (in {{days}} day(s)) is still unstaffed.",
                [French] = "Un service le {{date}} (dans {{days}} jour(s)) n'est toujours pas pourvu.",
                [Italian] = "Un turno il {{date}} (tra {{days}} giorno/i) è ancora scoperto."
            },
            [ProactiveMessageI18nKeys.WorkDroppedByErpImport] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Der Einsatz von {{employee}} am {{date}} ist entfallen, weil die zugehörige Bestellung per ERP-Import ersetzt wurde. Bitte neu einplanen.",
                [English] = "{{employee}}'s assignment on {{date}} was cancelled because its order was replaced by an ERP import. Please re-plan it.",
                [French] = "L'affectation de {{employee}} le {{date}} a été annulée car la commande correspondante a été remplacée par un import ERP. Merci de replanifier.",
                [Italian] = "L'incarico di {{employee}} del {{date}} è stato annullato perché l'ordine corrispondente è stato sostituito da un'importazione ERP. Si prega di ripianificare."
            },
            [ProactiveMessageI18nKeys.OrderImportFailed] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Der ERP-Import der Datei {{file}} ist fehlgeschlagen: {{reason}}",
                [English] = "The ERP import of file {{file}} failed: {{reason}}",
                [French] = "L'import ERP du fichier {{file}} a échoué : {{reason}}",
                [Italian] = "L'importazione ERP del file {{file}} non è riuscita: {{reason}}"
            },
            [ProactiveMessageI18nKeys.EscalationStageAlert] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "{{employee}} ist für den Dienst am {{date}} ausgefallen. Bitte antworte bis {{dueTime}}, ob du übernimmst.",
                [English] = "{{employee}} is out for the shift on {{date}}. Please reply by {{dueTime}} whether you can take it.",
                [French] = "{{employee}} est absent(e) pour le service du {{date}}. Merci de répondre avant {{dueTime}} si tu peux le prendre.",
                [Italian] = "{{employee}} non è disponibile per il turno del {{date}}. Rispondi entro le {{dueTime}} se puoi coprirlo."
            },
            [ProactiveMessageI18nKeys.DailyDigest] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Tages-Digest: {{totalCount}} offene Feststellung(en) - {{highCount}} hoch, {{mediumCount}} mittel, {{lowCount}} niedrig priorisiert ({{newCount}} neu seit gestern).",
                [English] = "Daily digest: {{totalCount}} open finding(s) - {{highCount}} high, {{mediumCount}} medium, {{lowCount}} low priority ({{newCount}} new since yesterday).",
                [French] = "Résumé quotidien : {{totalCount}} constat(s) ouvert(s) - {{highCount}} haute, {{mediumCount}} moyenne, {{lowCount}} basse priorité ({{newCount}} nouveau(x) depuis hier).",
                [Italian] = "Riepilogo giornaliero: {{totalCount}} rilevazione/i aperta/e - {{highCount}} alta, {{mediumCount}} media, {{lowCount}} bassa priorità ({{newCount}} nuova/e da ieri)."
            }
        };

    private static readonly LocalizedTextCatalogue Catalogue = new(CoreTexts);

    /// <summary>
    /// The languages whose sentences are authored in code rather than read from a pack's translations.json:
    /// MultiLanguage.CoreLanguages, so a fifth core language cannot be added without the catalogue guard
    /// going red.
    /// </summary>
    public static readonly IReadOnlyList<string> CoreLanguages = MultiLanguage.CoreLanguages;

    /// <summary>
    /// The i18n keys this catalogue covers, exposed so a test can assert it stays aligned with
    /// both MessengerWakeUpPolicy and the frontend catalogues, and so the pack loader knows which keys of a
    /// translations.json to read.
    /// </summary>
    public static IEnumerable<string> CoveredKeys => CoreTexts.Keys;

    /// <summary>Whether the messenger can carry this key at all.</summary>
    /// <param name="i18nKey">Key without the i18n marker prefix</param>
    public static bool Covers(string i18nKey) => !string.IsNullOrWhiteSpace(i18nKey) && CoreTexts.ContainsKey(i18nKey);

    /// <summary>
    /// Adds (or replaces) the sentences of one language pack. Called once per pack at startup by
    /// AssistantTextsPluginLoader with the CoveredKeys read from the pack's translations.json.
    /// </summary>
    /// <param name="languageCode">Locale of the pack the texts were read from</param>
    /// <param name="texts">The pack's sentences, keyed by i18n key</param>
    public static void Configure(string languageCode, IReadOnlyDictionary<string, string> texts) =>
        Catalogue.Configure(languageCode, texts);

    /// <summary>
    /// Looks up the sentence for an i18n key. False when the key is not covered or when a loaded pack lacks
    /// it; an unknown language (including one whose pack was never loaded) resolves to English.
    /// </summary>
    /// <param name="i18nKey">Key without the i18n marker prefix.</param>
    /// <param name="language">Installation language, possibly regional (de-CH), or null.</param>
    /// <param name="text">The sentence, still containing its placeholders.</param>
    public static bool TryGetText(string i18nKey, string? language, out string text) =>
        Catalogue.TryGetText(i18nKey, language, out text);

    /// <summary>The English sentence of a covered key: the floor when a loaded pack lacks a key.</summary>
    /// <param name="i18nKey">A covered key</param>
    public static string EnglishOf(string i18nKey) => CoreTexts[i18nKey][LanguageConfig.DefaultLanguageFallback];

    /// <summary>The per-language core variants of one key, for the guard tests.</summary>
    /// <param name="i18nKey">A covered key</param>
    internal static IReadOnlyDictionary<string, string> VariantsOf(string i18nKey) => Catalogue.VariantsOf(i18nKey);

    /// <summary>Discards every configured pack. Test-only, see LocalizedTextCatalogue.Reset.</summary>
    internal static void Reset() => Catalogue.Reset();
}
