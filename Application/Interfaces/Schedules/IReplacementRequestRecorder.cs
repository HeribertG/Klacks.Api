// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Write side of the replacement request book for the two creating paths: the proposals of a cover_absence run
/// and a manual replacement WorkChange. Both only stage rows; the caller's unit of work commits them together
/// with the rest of its writes. Re-recording an existing natural key never inserts a second row.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IReplacementRequestRecorder
{
    /// <summary>
    /// Stages one Proposed row per covered slot (in the run's scenario) and returns the slots enriched with the
    /// row id, the current outcome and the candidate's phone number. Slots without a shift are returned unchanged.
    /// </summary>
    /// <param name="context">Facts shared by the whole run (absent employee, absence type, token, source, report time)</param>
    /// <param name="covered">The slots the engine covered</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<CoveredSlot>> RecordProposalsAsync(
        ReplacementProposalContext context, IReadOnlyList<CoveredSlot> covered, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an Accepted ManualReplacement row for a replacement WorkChange the planner (or a chat skill) created.
    /// Does nothing for a non-replacement type, a change without ReplaceClientId, or a change the recovery engine
    /// materialised (tagged with RecoveryMarkers.WorkChangeSource - cover_absence records its own Proposed rows).
    /// The row carries the parent work's scenario token and the source shift (never the scenario clone); the
    /// slot start is placed on the calendar like the replacement window (after midnight of a night shift = next day).
    /// </summary>
    /// <param name="parentWork">The work the change is attached to</param>
    /// <param name="workChange">The change about to be saved (its id is already assigned)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordManualReplacementAsync(Work parentWork, WorkChange workChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keeps the ManualReplacement row of an updated WorkChange in step: records it when the change became a
    /// replacement, updates candidate, slot and times, and soft-deletes it when the change is no longer a
    /// replacement or another live row already owns the new natural key. Stages only.
    /// </summary>
    /// <param name="parentWork">The work the change is attached to (supplies the scenario token)</param>
    /// <param name="workChange">The change as it is about to be saved</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SyncManualReplacementAsync(Work parentWork, WorkChange workChange, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes the ManualReplacement row of a deleted WorkChange - the replacement did not happen. Stages only.
    /// </summary>
    /// <param name="workChangeId">Id of the deleted WorkChange</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DiscardManualReplacementAsync(Guid workChangeId, CancellationToken cancellationToken = default);
}