// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Delivers the mandatory report that follows every autonomous action. Separate from the notification
/// pipeline on purpose: IAgentTriggerService applies per-user mute, snooze and a daily rate limit, and
/// a report about something Klacksy has already DONE may not be droppable by any of them. This path
/// therefore mirrors ScheduledTaskRunner instead - the note is stashed durably first and only then, if
/// the recipient happens to be connected, also pushed live and acknowledged.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProactiveActionReporter
{
    /// <summary>
    /// Delivers one report to everybody an approved action concerns: the approver, plus the planning
    /// audience of the finding's group, or the admins when the finding has no group (Owner decision
    /// 2026-09-20). Each recipient gets their own stashed note; a recipient that cannot be resolved or
    /// stashed is logged and skipped, never allowed to unwind the tick. Returns how many notes were
    /// persisted.
    /// </summary>
    /// <param name="approverUserId">The human who released the action, always among the recipients; null before any approval exists, in which case the audience alone is told.</param>
    /// <param name="groupId">The finding's group, or null for a finding without one.</param>
    /// <param name="message">Report body; already composed, never a translation key.</param>
    Task<int> ReportToApprovalAudienceAsync(
        Guid? approverUserId, Guid? groupId, string message, CancellationToken cancellationToken = default);
}
