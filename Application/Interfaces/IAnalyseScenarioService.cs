// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Application contract for AnalyseScenario-scoped data operations that
/// require direct EF Core access (cloning, promoting, sweeping soft-deletes,
/// group hierarchy resolution). Keeps command handlers free of DataBaseContext.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

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
    /// are cloned even when they have no GroupItem in the resolved group hierarchy;
    /// they are added to the group's own shifts, and a listed clone supersedes its
    /// real root shift. With <paramref name="groupId"/> <c>null</c> the listed IDs
    /// are the exact set to clone.
    /// Returns the shift ID map (original shift ID → cloned shift ID) so callers
    /// can remap external shift references to the cloned IDs.
    /// <paramref name="sourceToken"/> builds the new scenario on an existing scenario instead of the real plan:
    /// inside the source scenario's own range its rows are copied (including its changes, breaks, replacements, temporary
    /// memberships and open replacement requests), outside that range the real plan's rows. Every clone keeps the real
    /// root shift in ScenarioSourceShiftId, so the new scenario is accepted straight into the real plan; the source stays
    /// an independent scenario that can still be accepted like any sibling scenario. The shift map then also maps each
    /// real root id to the clone that represents it. The source must be an active scenario, and with a grouped source
    /// the new scenario's group must lie in the source group's hierarchy (otherwise
    /// <see cref="Klacks.Api.Application.Exceptions.ConflictException"/>); a null group inherits the source's group. An
    /// unknown token throws <see cref="KeyNotFoundException"/>. Null keeps the real-plan clone unchanged.
    /// The source token is not checked for visibility here: the caller must have verified that the user may see the
    /// source scenario's group.
    /// </summary>
    Task<Dictionary<Guid, Guid>> CloneScenarioDataAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, Guid token, IReadOnlyCollection<Guid>? additionalShiftIds, Guid? sourceToken, CancellationToken cancellationToken);

    /// <summary>
    /// Same as <see cref="CloneScenarioDataAsync"/> but also returns the work ID map
    /// (original work ID → cloned work ID), so the apply path can re-point the cloned works
    /// in place (preserving their WorkChange/Expense/sub-Break children) instead of delete+recreate.
    /// </summary>
    Task<(Dictionary<Guid, Guid> ShiftIdMap, Dictionary<Guid, Guid> WorkIdMap)> CloneScenarioDataWithMapsAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, Guid token, IReadOnlyCollection<Guid>? additionalShiftIds, Guid? sourceToken, CancellationToken cancellationToken);

    /// <summary>
    /// Maps every scenario clone among <paramref name="shiftIds"/> to the real root shift it stands for
    /// (ScenarioSourceShiftId). Real shifts and unknown ids are left out, so a caller falls back to the id itself.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Guid>> ResolveRootShiftIdsAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken);

    /// <summary>
    /// Ids of every live shift of the scenario plus the real root shifts they stand for: the shifts whose works the
    /// scenario owns, in either id space (scenario rows carry the clone id, real rows the root id).
    /// </summary>
    Task<IReadOnlySet<Guid>> GetScenarioShiftIdsWithRootsAsync(Guid token, CancellationToken cancellationToken);

    /// <summary>
    /// Materialises a temporary cross-group membership under the scenario token: the client joins the
    /// receiving group for the [validFrom, validUntil] window. Promote makes it real; Reject discards it.
    /// </summary>
    Task AddScenarioMembershipAsync(Guid token, Guid clientId, Guid groupId, DateOnly validFrom, DateOnly validUntil, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes the real-side schedule rows (works, work_changes, expenses,
    /// breaks, schedule_notes) in the date range so the scenario clones can
    /// be promoted into place. When <paramref name="groupId"/> is <c>null</c>
    /// the scope is limited to the scenario's own footprint (the real shifts its works promote onto and the
    /// clients it touches); otherwise the scope is limited to the group hierarchy. Locked rows are never deleted.
    /// </summary>
    Task SoftDeleteRealScheduleDataAsync(Guid? groupId, Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken);

    /// <summary>
    /// Read-only staleness check before an accept: the real rows (works, work changes, expenses, breaks, schedule
    /// notes) inside exactly the scope <see cref="SoftDeleteRealScheduleDataAsync"/> replaces that were created,
    /// updated or deleted after the scenario was created. Null when there are none (the accept overwrites nothing
    /// newer than the scenario) or when the scenario has no creation time to compare against.
    /// </summary>
    /// <param name="scenario">The scenario about to be accepted (group, token, range and creation time are read)</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ScenarioStaleness?> FindRealPlanChangesSinceCreationAsync(AnalyseScenario scenario, CancellationToken cancellationToken);

    /// <summary>
    /// Validates that no real-side conflict prevents accepting the scenario.
    /// Throws ConflictException when a source shift was deleted or its
    /// child-count subtree changed since the scenario was created (e.g. a
    /// concurrent cut by another user), or when scenario works sit on a scenario
    /// shift that has no real root shift to be promoted onto.
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
    /// Ids of the live (not deleted) breaks that belong to the scenario. Must be read BEFORE
    /// <see cref="PromoteScenarioWorksAsync"/>, which clears the scenario token on them.
    /// </summary>
    /// <param name="token">Scenario isolation token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<Guid>> GetScenarioBreakIdsAsync(Guid token, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes the movable (LockLevel.None) cloned works on the <paramref name="plannedSlots"/>
    /// (shift, date) pairs the planner fills, plus their WorkChange/sub-Break children, so the planner's
    /// works REPLACE the incumbent on those slots instead of double-booking them on accept (grill H2).
    /// Expenses stay attached until <see cref="ResolveDisplacedWorksAsync"/> moves them. Movable works on
    /// slots the planner did NOT plan are preserved (the wizard run can cover a narrower agent/shift set
    /// than the group-scoped clone). Locked works (Confirmed/Approved/Closed), replacement covers and
    /// standalone absence breaks (ParentWorkId == null) are always left intact.
    /// </summary>
    /// <returns>Ids of the displaced works</returns>
    Task<IReadOnlyList<Guid>> SoftDeleteClonedWorksOnSlotsAsync(Guid token, DateOnly fromDate, DateOnly untilDate, IReadOnlySet<(Guid ShiftId, DateOnly Date)> plannedSlots, CancellationToken cancellationToken);

    /// <summary>
    /// Completes the slot replacement once the planner's works exist: a displaced work's expenses (taxable and not)
    /// move to the planner's work on the same slot (preferably the same employee), replacing the shift default expenses
    /// BulkAdd staged on it. A slot that received no planner work is emptied: its expenses follow the employee's own new
    /// work that day, and only without one does a displaced work carrying expenses stay with its children.
    /// WorkChanges of a replaced work are not moved (they are the displaced employee's own time).
    /// </summary>
    /// <param name="displacedWorkIds">Result of <see cref="SoftDeleteClonedWorksOnSlotsAsync"/></param>
    /// <param name="createdWorkIds">Works the planner created in the same scenario</param>
    /// <returns>Number of expenses that moved to a work of another employee</returns>
    Task<int> ResolveDisplacedWorksAsync(Guid token, IReadOnlyList<Guid> displacedWorkIds, IReadOnlyList<Guid> createdWorkIds, CancellationToken cancellationToken);
}
