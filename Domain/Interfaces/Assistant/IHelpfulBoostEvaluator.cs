// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Recomputes the per-user daily budget boost of a trigger kind from the user's persisted
/// reaction history. Called after every stored reaction so the boost rises with helpful
/// reactions and falls again once dismissals displace them.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IHelpfulBoostEvaluator
{
    Task EvaluateAsync(string userId, string triggerKind, CancellationToken cancellationToken = default);
}
