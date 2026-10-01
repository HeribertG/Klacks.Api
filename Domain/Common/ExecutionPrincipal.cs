// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Ambient id of the user on whose behalf deferred work runs outside an HTTP request (scheduled tasks,
/// background plan execution). Without it such work had no calling user and every visibility check treated it
/// as an unrestricted system job, although a group-restricted owner had started it. An AsyncLocal, like
/// TurnCorrelation, so it flows into child DI scopes and background continuations of the same async flow and
/// stays invisible to concurrent requests. Work that genuinely has no owner never sets it and stays unrestricted.
/// </summary>

namespace Klacks.Api.Domain.Common;

public static class ExecutionPrincipal
{
    private static readonly AsyncLocal<string?> CurrentUserIdValue = new();

    public static string? CurrentUserId => CurrentUserIdValue.Value;

    /// <summary>
    /// Runs the rest of the current async flow on behalf of the given user until the returned scope is disposed;
    /// the previous value is restored afterwards. An empty id leaves the flow without an owner.
    /// </summary>
    /// <param name="userId">The owner the deferred work acts for</param>
    public static IDisposable Begin(Guid userId)
    {
        var previous = CurrentUserIdValue.Value;
        CurrentUserIdValue.Value = userId == Guid.Empty ? null : userId.ToString();
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope(string? previous) : IDisposable
    {
        public void Dispose() => CurrentUserIdValue.Value = previous;
    }
}
