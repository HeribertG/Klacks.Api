// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public interface ISpeechGoldsetLoader
{
    Task<IReadOnlyList<SpeechGoldsetItem>> LoadAsync(string goldset, CancellationToken cancellationToken = default);

    string ResolveAudioPath(string audioFile);
}
