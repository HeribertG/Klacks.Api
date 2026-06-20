// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Curated pool of light, low-stakes "by the way" topics Klacksy may ask about to learn a personal
/// interest. Each entry pairs a stable topic key with a small set of keywords (multilingual) used to
/// detect whether the user has already revealed that interest, so Klacksy does not ask twice. The
/// actual question wording lives in the frontend i18n files (key <see cref="KeyFor"/>) and is
/// rendered in the user's UI language. Topics stay safe across cultures: no religion, politics,
/// money, health or anything personal-sensitive.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record CuriosityQuestion(string Topic, IReadOnlyList<string> Keywords);

public static class CuriosityQuestions
{
    public static readonly IReadOnlyList<CuriosityQuestion> Pool =
    [
        new("sport", ["sport", "football", "soccer", "fussball", "tennis", "hockey", "ski", "yoga", "gym", "running"]),
        new("music", ["music", "musik", "band", "song", "concert", "konzert", "guitar", "playlist"]),
        new("coffee_tea", ["coffee", "kaffee", "tea", "tee", "espresso", "cappuccino"]),
        new("travel", ["travel", "travelling", "reise", "reisen", "vacation", "ferien", "holiday", "urlaub"]),
        new("weekend", ["weekend", "wochenende", "hiking", "wandern", "garden", "garten", "family time"]),
        new("cooking", ["cooking", "cook", "kochen", "koch", "baking", "backen", "recipe", "rezept"]),
    ];

    /// <summary>Frontend i18n key that holds the question wording for a topic.</summary>
    public static string KeyFor(string topic) => $"assistant-chat.proactive.curiosity.{topic}";
}
