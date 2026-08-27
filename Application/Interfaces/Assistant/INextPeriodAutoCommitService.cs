// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Watches a detector-started AutoWizard job in the background and, once its chain has completed,
/// accepts the produced scenario into the real schedule — but ONLY when the scenario introduces
/// zero new compliance issues; otherwise the scenario is left as a draft and the planners are
/// notified. Used exclusively by the FullyAutonomous branch of NextPeriodSchedulingDueDetector.
/// </summary>
public interface INextPeriodAutoCommitService
{
    void QueueAutoCommit(Guid jobId, Guid groupId, string groupName, DateOnly periodStart, DateOnly periodEnd);
}
