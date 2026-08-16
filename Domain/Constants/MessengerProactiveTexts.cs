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
/// ============================ DUPLICATED TEXT - KEEP IN SYNC ============================
/// Every sentence below is a verbatim copy of the same key in the frontend catalogues:
///     Klacks.Ui/src/assets/i18n/de.json
///     Klacks.Ui/src/assets/i18n/en.json
///     Klacks.Ui/src/assets/i18n/fr.json
///     Klacks.Ui/src/assets/i18n/it.json
/// Affected keys: assistant.proactive.unstaffedShift, assistant.proactive.workDroppedByErpImport,
/// assistant.proactive.orderImportFailed.
/// Change one of those four files and the same edit belongs here, otherwise the inbox and the
/// messenger tell the same recipient two different sentences. The drift is caught by
/// MessengerProactiveTextsTests.CatalogueMatchesTheFrontendTranslationFiles, which reads the Ui
/// JSON files directly whenever the frontend repository sits next to this one.
/// =======================================================================================
///
/// Placeholders use the frontend's ngx-translate form ({{name}}) so the two catalogues stay
/// literally comparable; ProactiveMessengerTextComposer substitutes them from SummaryParams.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Constants;

public static class MessengerProactiveTexts
{
    public const string PlaceholderPrefix = "{{";
    public const string PlaceholderSuffix = "}}";

    private const string German = "de";
    private const string English = "en";
    private const string French = "fr";
    private const string Italian = "it";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Texts =
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
            }
        };

    /// <summary>
    /// The i18n keys this catalogue covers, exposed so a test can assert it stays aligned with
    /// both MessengerWakeUpPolicy and the frontend catalogues.
    /// </summary>
    public static IEnumerable<string> CoveredKeys => Texts.Keys;

    /// <summary>
    /// Looks up the sentence for an i18n key. Falls back to the installation fallback language
    /// when the requested language is not carried for that key, so a language pack installed after
    /// this table was written still yields a readable message instead of nothing.
    /// </summary>
    /// <param name="i18nKey">Key without the i18n marker prefix.</param>
    /// <param name="language">Two-letter language code to render in.</param>
    /// <param name="text">The sentence, still containing its placeholders.</param>
    public static bool TryGetText(string i18nKey, string language, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(i18nKey) || !Texts.TryGetValue(i18nKey, out var byLanguage))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(language) && byLanguage.TryGetValue(language, out var localized))
        {
            text = localized;
            return true;
        }

        if (byLanguage.TryGetValue(LanguageConfig.DefaultLanguageFallback, out var fallback))
        {
            text = fallback;
            return true;
        }

        return false;
    }
}
