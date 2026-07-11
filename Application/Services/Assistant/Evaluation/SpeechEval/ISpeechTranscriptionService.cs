// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public interface ISpeechTranscriptionService
{
    Task<string> TranscribeAsync(string providerId, byte[] audio, string language, CancellationToken cancellationToken = default);
}
