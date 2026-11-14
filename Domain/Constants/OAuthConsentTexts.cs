// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The sentences of the OAuth consent page, which the server renders itself and which therefore reach the user without a
/// model call and without the browser UI catalogue. The four core languages live in the table below, the 21 plugin
/// languages are merged in at startup from each pack's assistant-texts.json by AssistantTextsPluginLoader (the key IS the
/// pack key). Resolution is LocalizedTextCatalogue's. Two keys carry placeholders: LocalProgramWarning has {target}
/// (host and port of the program on this computer), Lifetime has {duration}, which is DurationDays or DurationHours.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Constants;

public static class OAuthConsentTexts
{
    private const string German = "de";
    private const string English = "en";
    private const string French = "fr";
    private const string Italian = "it";

    public const string TargetPlaceholder = "{target}";
    public const string DurationPlaceholder = "{duration}";

    public const string Title = "oauth.consent.title";
    public const string HostLabel = "oauth.consent.hostLabel";
    public const string Intro = "oauth.consent.intro";
    public const string LocalProgramWarning = "oauth.consent.localProgramWarning";
    public const string AccessLegend = "oauth.consent.accessLegend";
    public const string AccessRead = "oauth.consent.accessRead";
    public const string AccessWrite = "oauth.consent.accessWrite";
    public const string Lifetime = "oauth.consent.lifetime";
    public const string DurationDays = "oauth.consent.durationDays";
    public const string DurationHours = "oauth.consent.durationHours";
    public const string EmailLabel = "oauth.consent.emailLabel";
    public const string PasswordLabel = "oauth.consent.passwordLabel";
    public const string Allow = "oauth.consent.allow";
    public const string Deny = "oauth.consent.deny";
    public const string ErrorFieldsRequired = "oauth.consent.error.fieldsRequired";
    public const string ErrorInvalidCredentials = "oauth.consent.error.invalidCredentials";
    public const string ErrorAccountNotAllowed = "oauth.consent.error.accountNotAllowed";
    public const string ErrorSetupRequired = "oauth.consent.error.setupRequired";

    /// <summary>Every key a language pack has to ship in assistant-texts.json. The catalogue guard reads exactly this list.</summary>
    public static readonly IReadOnlyList<string> RequiredKeys =
    [
        Title, HostLabel, Intro, LocalProgramWarning, AccessLegend, AccessRead, AccessWrite, Lifetime, DurationDays,
        DurationHours, EmailLabel, PasswordLabel, Allow, Deny, ErrorFieldsRequired, ErrorInvalidCredentials,
        ErrorAccountNotAllowed, ErrorSetupRequired
    ];

    /// <summary>The languages whose texts are authored in code rather than shipped by a pack.</summary>
    public static readonly IReadOnlyList<string> CoreLanguages = MultiLanguage.CoreLanguages;

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> CoreTexts =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            [Title] = Texts(
                "Externes KI-Werkzeug mit Klacks verbinden",
                "Connect an external AI tool to Klacks",
                "Connecter un outil d'IA externe à Klacks",
                "Collega uno strumento IA esterno a Klacks"),
            [HostLabel] = Texts(
                "Der Zugriff wird an diese Adresse übergeben",
                "Access will be handed to this address",
                "L'accès sera transmis à cette adresse",
                "L'accesso verrà consegnato a questo indirizzo"),
            [Intro] = Texts(
                "Das Werkzeug handelt in deinem Namen mit den Rechten, die du unten wählst. Fahre nur fort, wenn du diese Verbindung gerade selbst gestartet hast.",
                "The tool will act in your name with the rights you choose below. Continue only if you have just started this connection yourself.",
                "L'outil agira en ton nom avec les droits que tu choisis ci-dessous. Ne continue que si tu viens de lancer cette connexion toi-même.",
                "Lo strumento agirà a tuo nome con i diritti che scegli qui sotto. Continua solo se hai appena avviato tu stesso questo collegamento."),
            [LocalProgramWarning] = Texts(
                "Das ist ein Programm auf diesem Computer ({target}). Wer auf diesem Computer Programme ausführen kann, könnte diesen Zugriff erhalten. Fahre nur fort, wenn du die Verbindung gerade selbst aus deinem eigenen Programm gestartet hast.",
                "This is a program on this computer ({target}). Anyone who can run programs on this computer could receive this access. Continue only if you have just started the connection from your own program.",
                "Il s'agit d'un programme sur cet ordinateur ({target}). Toute personne pouvant exécuter des programmes sur cet ordinateur pourrait recevoir cet accès. Ne continue que si tu viens de lancer la connexion depuis ton propre programme.",
                "Questo è un programma su questo computer ({target}). Chiunque possa eseguire programmi su questo computer potrebbe ricevere questo accesso. Continua solo se hai appena avviato il collegamento dal tuo programma."),
            [AccessLegend] = Texts(
                "Was darf das Werkzeug tun?",
                "What may the tool do?",
                "Que peut faire l'outil ?",
                "Cosa può fare lo strumento?"),
            [AccessRead] = Texts(
                "Nur lesen: Pläne, Mitarbeiter und weitere Daten ansehen, aber nichts ändern",
                "Read only: look at plans, employees and other data, but change nothing",
                "Lecture seule : consulter les plannings, les employés et d'autres données, sans rien modifier",
                "Sola lettura: vedere piani, dipendenti e altri dati, senza modificare nulla"),
            [AccessWrite] = Texts(
                "Lesen und ändern: zusätzlich Daten in deinem Namen ändern, zum Beispiel Pläne und Einträge",
                "Read and change: also change data in your name, for example plans and entries",
                "Lecture et modification : modifier aussi des données en ton nom, par exemple des plannings et des saisies",
                "Lettura e modifica: modificare anche dati a tuo nome, ad esempio piani e voci"),
            [Lifetime] = Texts(
                "Die Verbindung gilt {duration}. Du kannst sie früher beenden, indem du das Zugriffstoken in Klacks widerrufst.",
                "The connection is valid for {duration}. You can end it earlier by revoking the access token in Klacks.",
                "La connexion est valable {duration}. Tu peux y mettre fin plus tôt en révoquant le jeton d'accès dans Klacks.",
                "Il collegamento è valido per {duration}. Puoi terminarlo prima revocando il token di accesso in Klacks."),
            [DurationDays] = Texts("30 Tage", "30 days", "30 jours", "30 giorni"),
            [DurationHours] = Texts("24 Stunden", "24 hours", "24 heures", "24 ore"),
            [EmailLabel] = Texts("E-Mail", "Email", "E-mail", "E-mail"),
            [PasswordLabel] = Texts("Passwort", "Password", "Mot de passe", "Password"),
            [Allow] = Texts("Zugriff erlauben", "Allow access", "Autoriser l'accès", "Consenti l'accesso"),
            [Deny] = Texts("Ablehnen", "Deny", "Refuser", "Rifiuta"),
            [ErrorFieldsRequired] = Texts(
                "E-Mail und Passwort sind erforderlich.",
                "Email and password are required.",
                "L'e-mail et le mot de passe sont obligatoires.",
                "E-mail e password sono obbligatorie."),
            [ErrorInvalidCredentials] = Texts(
                "Ungültige E-Mail oder ungültiges Passwort.",
                "Invalid email or password.",
                "E-mail ou mot de passe invalide.",
                "E-mail o password non valide."),
            [ErrorAccountNotAllowed] = Texts(
                "Dieses Konto darf keine externen Werkzeuge verbinden. Ein Supervisor- oder Administrator-Konto ist nötig.",
                "This account may not connect external tools. A supervisor or administrator account is required.",
                "Ce compte ne peut pas connecter d'outils externes. Un compte superviseur ou administrateur est nécessaire.",
                "Questo account non può collegare strumenti esterni. Serve un account supervisore o amministratore."),
            [ErrorSetupRequired] = Texts(
                "Das vorgegebene Admin-Konto muss durch dein eigenes Admin-Konto ersetzt werden, bevor ein externes Werkzeug verbunden werden kann.",
                "The seeded admin account must be replaced with your own admin account before an external tool can be connected.",
                "Le compte administrateur par défaut doit être remplacé par ton propre compte administrateur avant de pouvoir connecter un outil externe.",
                "L'account amministratore predefinito deve essere sostituito con il tuo account amministratore prima di poter collegare uno strumento esterno.")
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

    /// <summary>Resolves the text for a language; only an unknown language falls back to English.</summary>
    /// <param name="key">One of RequiredKeys</param>
    /// <param name="language">Language tag, possibly regional (de-CH), or null</param>
    /// <param name="text">The resolved text, empty when nothing resolves</param>
    public static bool TryGetText(string key, string? language, out string text) =>
        Catalogue.TryGetText(key, language, out text);

    /// <summary>The language code that claims a tag (full tag, else base language), or null when no table and no pack does.</summary>
    /// <param name="language">Language tag, possibly regional (de-CH), or null</param>
    public static string? ResolveClaimedLanguage(string? language) => Catalogue.ResolveClaimedLanguage(language);

    /// <summary>The English text of a key.</summary>
    /// <param name="key">One of RequiredKeys</param>
    public static string EnglishOf(string key) => CoreTexts[key][LanguageConfig.DefaultLanguageFallback];

    /// <summary>Every key of the core catalogue, for the guard tests.</summary>
    internal static IReadOnlyCollection<string> Keys => Catalogue.Keys;

    /// <summary>The per-language core variants of one key, for the guard tests.</summary>
    /// <param name="key">One of RequiredKeys</param>
    internal static IReadOnlyDictionary<string, string> VariantsOf(string key) => Catalogue.VariantsOf(key);

    /// <summary>Discards every configured pack. Test-only, see LocalizedTextCatalogue.Reset.</summary>
    internal static void Reset() => Catalogue.Reset();

    private static IReadOnlyDictionary<string, string> Texts(string german, string english, string french, string italian) =>
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [German] = german,
            [English] = english,
            [French] = french,
            [Italian] = italian
        };
}
