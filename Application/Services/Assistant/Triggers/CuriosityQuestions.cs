// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Curated pool of light, low-stakes "by the way" questions Klacksy may ask to learn a personal
/// interest. Each entry pairs a stable topic key with warm, on-brand question text and a small set
/// of keywords (multilingual) used to detect whether the user has already revealed that interest,
/// so Klacksy does not ask twice. Topics stay safe across cultures: no religion, politics, money,
/// health or anything personal-sensitive.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record CuriosityQuestion(string Topic, string Text, IReadOnlyList<string> Keywords);

public static class CuriosityQuestions
{
    public static readonly IReadOnlyList<CuriosityQuestion> Pool =
    [
        new("sport", "By the way — are you into any sport? I'm always a little curious what people follow when they're off the clock.",
            ["sport", "football", "soccer", "fussball", "tennis", "hockey", "ski", "yoga", "gym", "running"]),
        new("music", "Out of curiosity: what kind of music gets you through a long planning day?",
            ["music", "musik", "band", "song", "concert", "konzert", "guitar", "playlist"]),
        new("coffee_tea", "Quick one between tasks — are you team coffee or team tea? I file these things away.",
            ["coffee", "kaffee", "tea", "tee", "espresso", "cappuccino"]),
        new("travel", "By the way, do you enjoy travelling? Anywhere that has stuck with you?",
            ["travel", "travelling", "reise", "reisen", "vacation", "ferien", "holiday", "urlaub"]),
        new("weekend", "When the roster is finally sorted, how do you most like to spend a free weekend?",
            ["weekend", "wochenende", "hiking", "wandern", "garden", "garten", "family time"]),
        new("cooking", "Random thought while it's quiet: are you someone who enjoys cooking, or more of a takeout person?",
            ["cooking", "cook", "kochen", "koch", "baking", "backen", "recipe", "rezept"]),
    ];
}
