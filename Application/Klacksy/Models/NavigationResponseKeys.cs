// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// i18n keys and short pre-translated acknowledgments for deterministic chat
/// responses emitted by the Klacksy navigate_to fast-path.
/// </summary>
namespace Klacks.Api.Application.Klacksy.Models;

public static class NavigationResponseKeys
{
    public const string SuccessScrolled = "nav.success.scrolled";
    public const string SuccessRouted = "nav.success.routed";
    public const string FailTargetMissing = "nav.fail.targetMissing";
    public const string FailPermissionDenied = "nav.fail.permissionDenied";
    public const string EmptyUtteranceGreeting = "nav.greeting.empty";

    /// <summary>
    /// Short localized acknowledgment emitted as a content chunk during a Tier-1
    /// fast-path navigation. Gives the user a visible bot message while the
    /// router performs the redirect. Falls back to English for unknown locales.
    /// </summary>
    /// <param name="locale">User locale (de/en/fr/it).</param>
    public static string FastPathAck(string locale) => locale switch
    {
        "de" => "Ich öffne die Seite für dich.",
        "fr" => "J'ouvre la page pour toi.",
        "it" => "Apro la pagina per te.",
        _ => "Opening the page for you."
    };
}
