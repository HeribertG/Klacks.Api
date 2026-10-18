// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Pre-write checks a Work must pass before it is (re-)placed into the real plan: the sporadic-shift
/// capacity and the one conflict class that can never be reported after the fact. Each check exists twice:
/// a Find method that only reports the refusal, and an Ensure method that throws exactly that refusal, so a
/// diagnosis and the write path share one decision. The sporadic check runs on create and restore only.
/// </summary>
public interface IWorkWriteGuard
{
    /// <summary>
    /// The refusal when the Work's shift is sporadic and its day or range capacity is used up, otherwise null.
    /// </summary>
    /// <param name="work">The Work about to be written; its own id is excluded from the usage count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<WorkBookingRefusal?> FindSporadicRefusalAsync(Work work, CancellationToken cancellationToken);

    /// <summary>
    /// Throws a WorkWriteConflictException when <see cref="FindSporadicRefusalAsync"/> reports a refusal.
    /// </summary>
    /// <param name="work">The Work about to be written; its own id is excluded from the usage count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureNoSporadicConflictAsync(Work work, CancellationToken cancellationToken);

    /// <summary>
    /// The refusal when the planned rows would introduce a hard-blocking schedule conflict (a missing mandatory
    /// qualification or one held below a binding minimum level), otherwise null. The caller decides which rows
    /// are checked at all (scenario rows and container children are skipped by the write paths).
    /// </summary>
    /// <param name="plannedRows">The rows as they would be after the write, checked together in one pass</param>
    /// <param name="vacatedRows">Rows the same write removes (the pre-write row of an edit or reassignment), or null</param>
    /// <param name="operation">The write path, which words the refusal message exactly as that path always has</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<WorkBookingRefusal?> FindHardBlockingRefusalAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        IReadOnlyList<PlannedRemovalRow>? vacatedRows,
        WorkWriteOperation operation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Throws a WorkWriteConflictException when the Work would introduce a hard-blocking schedule conflict.
    /// Skipped for scenario writes, mirroring the day-lock contract.
    /// </summary>
    /// <param name="work">The Work about to be written</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureNoHardBlockingConflictAsync(Work work, CancellationToken cancellationToken);
}
