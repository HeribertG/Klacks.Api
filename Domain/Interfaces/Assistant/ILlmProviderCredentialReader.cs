// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only access to a configured LLM provider's API key, keyed by provider id (e.g. "google").
/// Used by embedding providers that need a key already stored for chat (via the LLM provider
/// settings UI/DB) instead of a separate app-config secret.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILlmProviderCredentialReader
{
    /// <summary>
    /// Returns the provider's API key, or null when the provider is missing, disabled, or has no key.
    /// </summary>
    Task<string?> GetApiKeyAsync(string providerId, CancellationToken cancellationToken = default);
}
