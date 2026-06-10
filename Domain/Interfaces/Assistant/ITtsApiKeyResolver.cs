// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the API key for a cloud TTS provider, preferring the dedicated speech setting
/// and falling back to the legacy LLM provider credential for backward compatibility.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ITtsApiKeyResolver
{
    Task<string?> ResolveAsync(string providerId, CancellationToken ct = default);
}
