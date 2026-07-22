// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Heuristic id-substring denylist that identifies provider models which are not
/// chat/completions capable (embeddings, speech-to-text, text-to-speech, image, moderation,
/// realtime and legacy base-completion models). The model sync uses it to skip the
/// chat-completion probe for models that would predictably return an incompatibility error,
/// so they are represented as disabled rows without emitting a failed-probe log on every cycle.
/// The list is derived from the OpenAI model ecosystem (whose GET /models response carries no
/// capability metadata, so a data-driven filter is not possible) and is intentionally
/// conservative and easy to extend; matching is a case-insensitive substring test. Active
/// models are never re-evaluated by this filter, so a false positive can only withhold
/// auto-enable of a newly discovered model, never disable a live one.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class NonChatModelPatterns
{
    public const string SkipReason =
        "Not a chat/completions model (id matched a non-chat pattern); skipped, not tested";

    private const string Embed = "embed";
    private const string Whisper = "whisper";
    private const string Transcribe = "transcribe";
    private const string TextToSpeech = "tts";
    private const string Audio = "audio";
    private const string DallE = "dall-e";
    private const string Image = "image";
    private const string Moderation = "moderation";
    private const string Realtime = "realtime";
    private const string Davinci = "davinci";
    private const string Babbage = "babbage";

    private static readonly string[] ExcludedSubstrings =
    [
        Embed,
        Whisper,
        Transcribe,
        TextToSpeech,
        Audio,
        DallE,
        Image,
        Moderation,
        Realtime,
        Davinci,
        Babbage,
    ];

    public static bool IsLikelyNonChatModel(string? apiModelId)
    {
        if (string.IsNullOrWhiteSpace(apiModelId))
        {
            return false;
        }

        foreach (var pattern in ExcludedSubstrings)
        {
            if (apiModelId.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
