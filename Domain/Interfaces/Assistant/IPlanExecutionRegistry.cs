// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// In-memory registry of running AgentPlan executions, keyed by plan id. Lets the abort endpoint
/// request cooperative cancellation of a plan whose fire-and-forget executor is still running.
/// The executor checks the token between steps and finalizes the plan to 'aborted'.
/// NOTE: this registry is process-local. Klacks runs as a single application instance (docker
/// compose), so an executing plan and its abort request always share the same process. If the app
/// is ever scaled out, aborting a running plan on a different instance would miss the token; the
/// paused/drafting abort path (direct status write) is unaffected.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPlanExecutionRegistry
{
    /// <summary>
    /// Registers a new cancellation source for the plan and returns its token. Replaces (and
    /// disposes) any token previously registered for the same plan id.
    /// </summary>
    /// <param name="planId">Plan whose execution is about to start.</param>
    CancellationToken Register(Guid planId);

    /// <summary>
    /// Removes and disposes the plan's cancellation source. Safe to call when nothing is registered.
    /// </summary>
    /// <param name="planId">Plan whose execution has finished.</param>
    void Unregister(Guid planId);

    /// <summary>
    /// Requests cooperative cancellation of a currently running plan.
    /// </summary>
    /// <param name="planId">Plan to cancel.</param>
    /// <returns>True when a running execution was found and cancellation was signalled.</returns>
    bool TryRequestCancellation(Guid planId);
}
