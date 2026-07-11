// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants shared across the speech-wer evaluation pipeline.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public static class SpeechEvalConstants
{
    public const string GoldsetName = "speech-wer-v1";
    public const string NoMeasurableItemsMessage =
        "No audio files found for any goldset item; nothing was measured and no EvalRun was persisted. " +
        "Record the files listed in the goldset under Application/Skills/Goldsets/SpeechAudio/.";
}
