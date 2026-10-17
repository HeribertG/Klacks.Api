// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Resolves how deep Klacksy explains for a user (persisted override, usage heuristic or beginner default) and drops a
/// user's cached result after the override changed. Lives in the Application layer: only chat entry points and skills
/// consume it, the Domain pipeline reads the resolved profile from the context.
/// </summary>
public interface IExplainDepthResolver
{
    Task<ExplainDepthProfile?> ResolveAsync(string? userId, CancellationToken cancellationToken = default);

    void Invalidate(Guid userId);
}
