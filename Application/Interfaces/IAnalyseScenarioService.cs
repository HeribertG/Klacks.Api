// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Application contract for AnalyseScenario-scoped data operations that
/// require direct EF Core access (cloning, promoting, sweeping soft-deletes,
/// group hierarchy resolution). Keeps command handlers free of DataBaseContext.
/// </summary>

namespace Klacks.Api.Application.Interfaces;

public interface IAnalyseScenarioService
{
    /// <summary>
    /// Returns the group id plus every descendant group id (BFS over
    /// <c>group.parent</c>). Used to resolve the scope of a scenario.
    /// </summary>
    Task<List<Guid>> GetGroupHierarchyIdsAsync(Guid groupId, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes every row carrying the scenario token (works, work_changes,
    /// expenses, breaks, shifts, schedule_notes) plus any original-side
    /// sub-work or sub-break whose ParentWorkId falls inside the scenario's
    /// work set. This orphan sweep prevents zombie rows from re-appearing as
    /// parentless top-level entries in the next scenario clone.
    /// </summary>
    Task SoftDeleteScenarioDataAsync(Guid token, CancellationToken cancellationToken);

    /// <summary>
    /// Clones shifts, works (top-level + sub-works with ParentWorkId mapping),
    /// work_changes, expenses, breaks (including sub-breaks) and schedule_notes
    /// in the date range under the new scenario token. When <paramref name="groupId"/>
    /// is <c>null</c> every non-scenario row in the range is cloned; otherwise
    /// the full group hierarchy (group plus descendants) filters what is copied.
    /// <paramref name="additionalShiftIds"/> guarantees that the listed shift IDs
    /// are cloned even when they have no GroupItem in the resolved group hierarchy.
    /// Returns the shift ID map (original shift ID → cloned shift ID) so callers
    /// can remap external shift references to the cloned IDs.
    /// </summary>
    Task<Dictionary<Guid, Guid>> CloneScenarioDataAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, Guid token, IReadOnlyCollection<Guid>? additionalShiftIds, CancellationToken cancellationToken);

    /// <summary>
    /// Same as <see cref="CloneScenarioDataAsync"/> but also returns the work ID map
    /// (original work ID → cloned work ID), so the apply path can re-point the cloned works
    /// in place (preserving their WorkChange/Expense/sub-Break children) instead of delete+recreate.
    /// </summary>
    Task<(Dictionary<Guid, Guid> ShiftIdMap, Dictionary<Guid, Guid> WorkIdMap)> CloneScenarioDataWithMapsAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, Guid token, IReadOnlyCollection<Guid>? additionalShiftIds, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes the real-side schedule rows (works, work_changes, expenses,
    /// breaks, schedule_notes) in the date range so the scenario clones can
    /// be promoted into place. When <paramref name="groupId"/> is <c>null</c>
    /// every real row in the range is soft-deleted; otherwise the scope is
    /// limited to the group hierarchy.
    /// </summary>
    Task SoftDeleteRealScheduleDataAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken);

    /// <summary>
    /// Validates that no real-side conflict prevents accepting the scenario.
    /// Throws ConflictException when a source shift was deleted or its
    /// child-count subtree changed since the scenario was created (e.g. a
    /// concurrent cut by another user).
    /// </summary>
    Task ValidateNoAcceptConflictsAsync(Guid token, CancellationToken cancellationToken);

    /// <summary>
    /// Promotes scenario works/breaks/schedule-notes within the period to real
    /// by clearing their AnalyseToken and remapping each Work.ShiftId from the
    /// clone shift back to its real source shift via ScenarioSourceShiftId.
    /// Boundary clones (outside <paramref name="fromDate"/>..<paramref name="untilDate"/>)
    /// were context-only and are soft-deleted instead of promoted, so they do
    /// not duplicate the unchanged real entries that already exist in the
    /// boundary. Clone shifts, clone preferences and clone shift-expenses
    /// are soft-deleted (NOT promoted) — originals remain unchanged.
    /// </summary>
    /// <param name="token">Scenario isolation token.</param>
    /// <param name="fromDate">Period start (inclusive); entries with CurrentDate &lt; this are boundary.</param>
    /// <param name="untilDate">Period end (inclusive); entries with CurrentDate &gt; this are boundary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PromoteScenarioWorksAsync(Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes the movable (LockLevel.None) cloned works on the <paramref name="plannedSlots"/>
    /// (shift, date) pairs the planner fills, plus their WorkChange/Expense/sub-Break children, so the
    /// planner's works REPLACE the incumbent on those slots instead of double-booking them on accept
    /// (grill H2). Movable works on slots the planner did NOT plan are preserved (the wizard run can cover
    /// a narrower agent/shift set than the group-scoped clone). Locked works (Confirmed/Approved/Closed)
    /// and standalone absence breaks (ParentWorkId == null) are always left intact.
    /// </summary>
    Task SoftDeleteClonedWorksOnSlotsAsync(Guid token, DateOnly fromDate, DateOnly untilDate, IReadOnlySet<(Guid ShiftId, DateOnly Date)> plannedSlots, CancellationToken cancellationToken);
}
