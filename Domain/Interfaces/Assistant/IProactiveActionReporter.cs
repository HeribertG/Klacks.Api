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
    /// Stashes one report for one recipient and pushes it live when possible. Never throws: the report
    /// is the last step of an action that has already happened, so failing to deliver it must not
    /// unwind the tick. Returns whether the note was persisted.
    /// </summary>
    /// <param name="recipientUserId">The responsible owner from the kind's governance rule.</param>
    /// <param name="message">Report body; already composed, never a translation key.</param>
    Task<bool> ReportAsync(Guid recipientUserId, string message, CancellationToken cancellationToken = default);
}
