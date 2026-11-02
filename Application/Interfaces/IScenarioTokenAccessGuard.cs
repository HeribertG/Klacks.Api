// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Single place that decides whether the caller may work in the scenario an analyse token selects. A null token is
/// the main plan and always allowed; a token of a missing, deleted or group-hidden scenario is answered exactly like
/// a token that does not exist. Admins and background callers without a user are unrestricted and never refused for
/// a scenario or an unknown token. The token of a variant (key) is the exception: IsVisibleAsync and EnsureVisibleAsync
/// refuse it for every caller, admins and background callers included, because a variant carries shifts only. The
/// only door to it is IsVariantTokenVisibleAsync, which actions marked with AllowVariantToken use.
/// </summary>
public interface IScenarioTokenAccessGuard
{
    /// <param name="analyseToken">Scenario isolation token; null selects the main plan</param>
    Task<bool> IsVisibleAsync(Guid? analyseToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws KeyNotFoundException (404) when the token selects a scenario the caller may not see, a variant (key) or
    /// a scenario that does not exist.
    /// </summary>
    /// <param name="analyseToken">Scenario isolation token; null selects the main plan</param>
    Task EnsureVisibleAsync(Guid? analyseToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// True only when the token belongs to a variant (key) whose group the caller may see; false for a scenario token,
    /// an unknown token and a variant of a hidden group. Unrestricted callers see every variant.
    /// </summary>
    /// <param name="analyseToken">Token of the variant the action works in</param>
    Task<bool> IsVariantTokenVisibleAsync(Guid analyseToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// True only when the token belongs to a variant (key) whose group the caller may not see, so a caller can answer
    /// it like a missing row; false for a visible variant, a scenario token and an unknown token.
    /// </summary>
    /// <param name="analyseToken">Token stored on the row the caller loaded</param>
    Task<bool> IsHiddenVariantTokenAsync(Guid analyseToken, CancellationToken cancellationToken = default);
}
