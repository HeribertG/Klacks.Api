// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IDismissStreakEvaluator
{
    Task EvaluateAsync(string userId, string triggerKind, CancellationToken cancellationToken = default);
}
