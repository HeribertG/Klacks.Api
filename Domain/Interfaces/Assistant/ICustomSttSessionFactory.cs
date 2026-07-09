// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates STT sessions for user-configured custom providers (e.g. a self-hosted Whisper
/// server with an OpenAI-compatible transcription API) and validates their reachability.
/// </summary>
/// <param name="provider">The custom provider configuration (URL, optional API key, model)</param>
/// <param name="locale">Requested transcription locale (mapped to a Whisper language code)</param>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ICustomSttSessionFactory
{
    ISttSession CreateSession(CustomSttProvider provider, string locale);

    Task ValidateAsync(CustomSttProvider provider, CancellationToken ct = default);
}
