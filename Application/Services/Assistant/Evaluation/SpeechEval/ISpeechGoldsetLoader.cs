// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public interface ISpeechGoldsetLoader
{
    Task<IReadOnlyList<SpeechGoldsetItem>> LoadAsync(string goldset, CancellationToken cancellationToken = default);

    string ResolveAudioPath(string audioFile);
}
