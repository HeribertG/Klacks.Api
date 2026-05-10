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
    /// Promotes scenario works/breaks/schedule-notes to real by clearing
    /// their AnalyseToken and remapping each Work.ShiftId from the clone
    /// shift back to its real source shift via ScenarioSourceShiftId.
    /// Clone shifts, clone preferences and clone shift-expenses are
    /// soft-deleted (NOT promoted) — originals remain unchanged.
    /// </summary>
    Task PromoteScenarioWorksAsync(Guid token, CancellationToken cancellationToken);
}
