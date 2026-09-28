// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public interface ISpeechTranscriptionService
{
    Task<string> TranscribeAsync(string providerId, byte[] audio, string language, CancellationToken cancellationToken = default);
}
