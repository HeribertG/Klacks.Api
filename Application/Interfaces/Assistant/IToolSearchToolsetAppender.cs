// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Adds find_tools to an assembled toolset when the tool search is switched on, above the provider cap, so no
/// retrieved skill gives up its place for it.
/// </summary>
public interface IToolSearchToolsetAppender
{
    /// <summary>
    /// Adds the find_tools function to the assembled toolset when the tool search is switched on.
    /// </summary>
    /// <param name="agent">The agent whose enabled skills hold find_tools</param>
    /// <param name="toolset">The assembled toolset; find_tools is added to its functions</param>
    /// <param name="cancellationToken">Cancellation of the turn</param>
    Task AppendAsync(Agent agent, SkillToolsetResult toolset, CancellationToken cancellationToken = default);
}
