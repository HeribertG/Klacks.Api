// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Provides the cached identity prompt (global rules + soul sections) for an agent.
/// </summary>
/// <param name="agentId">The agent whose identity context to build</param>
/// <param name="language">UI language code for template variable resolution</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IIdentityContextProvider
{
    Task<string> GetIdentityPromptAsync(Guid agentId, string? language, CancellationToken cancellationToken = default);
}
