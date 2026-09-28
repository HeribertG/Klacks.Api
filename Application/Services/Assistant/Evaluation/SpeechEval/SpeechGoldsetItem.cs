// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One dictation item of a speech-wer goldset: the reference transcript, the proper names
/// that must survive transcription and the relative path of the recorded audio file.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public class SpeechGoldsetItem
{
    public string Id { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public string ReferenceText { get; set; } = string.Empty;

    public List<string> ExpectedNames { get; set; } = new();

    public string AudioFile { get; set; } = string.Empty;
}
