// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Root document of a speech-wer goldset JSON file.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public class SpeechGoldsetDocument
{
    public int Version { get; set; }

    public string? Kind { get; set; }

    public List<SpeechGoldsetItem> Items { get; set; } = new();
}
