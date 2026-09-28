// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IDismissStreakEvaluator
{
    Task EvaluateAsync(string userId, string triggerKind, CancellationToken cancellationToken = default);
}
