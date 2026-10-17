// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Services.Shifts;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Cuts shifts into pieces and resets the cuts of a sealed order. Every shift a batch touches (updated shift, stored
/// parent, referenced sealed order) and the reset order must be visible to a group-restricted caller; a hidden one is
/// answered with exactly the error a missing one gets, before anything is validated or written.
/// </summary>
/// <param name="shiftVisibilityGuard">Decides whether the caller may touch a shift and the groups a piece changes</param>
public class ShiftCutFacade : IShiftCutFacade
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftRequiredQualificationRepository _requirementRepository;
    private readonly IShiftTreeService _shiftTreeService;
    private readonly IShiftResetService _shiftResetService;
    private readonly IShiftValidator _shiftValidator;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly ILogger<ShiftCutFacade> _logger;

    public ShiftCutFacade(
        IShiftRepository shiftRepository,
        IShiftRequiredQualificationRepository requirementRepository,
        IShiftTreeService shiftTreeService,
        IShiftResetService shiftResetService,
        IShiftValidator shiftValidator,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ILogger<ShiftCutFacade> logger)
    {
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _shiftRepository = shiftRepository;
        _requirementRepository = requirementRepository;
        _shiftTreeService = shiftTreeService;
        _shiftResetService = shiftResetService;
        _shiftValidator = shiftValidator;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<List<Shift>> ProcessBatchCutsAsync(List<CutOperation> operations)
    {
        _logger.LogInformation("=== ProcessBatchCuts START (2-Phase Approach) ===");
        _logger.LogInformation("Number of operations: {OpCount}", operations.Count);

        await EnsureOperationsVisibleAsync(operations);
        await ValidateCutDates(operations);

        var processedShifts = new List<Shift>();
        var shiftsToDelete = new HashSet<Guid>();
        var requirementsOfCreated = new Dictionary<Guid, List<ShiftRequiredQualification>>();

        var sortedOps = TopologicalSort(operations);

        _logger.LogInformation("=== PHASE 1: Processing operations and setting hierarchy relations ===");

        foreach (var op in sortedOps)
        {
            if (op.Type == "UPDATE")
            {
                _logger.LogInformation("Processing UPDATE for ShiftId={ShiftId}", op.Data.Id);
                var updatedShift = await ProcessUpdate(op, shiftsToDelete, processedShifts);
                processedShifts.Add(updatedShift);
            }
            else if (op.Type == "CREATE")
            {
                _logger.LogInformation("Processing CREATE with ShiftId={ShiftId}, ParentId={ParentId}",
                    op.Data.Id, op.ParentId);
                var createdShift = await ProcessCreate(op, requirementsOfCreated);
                processedShifts.Add(createdShift);
            }
            else
            {
                throw new InvalidOperationException($"Unknown operation type: {op.Type}");
            }
        }

        foreach (var shiftId in shiftsToDelete)
        {
            _logger.LogInformation("Deleting original shift: {ShiftId}", shiftId);
            await _shiftRepository.Delete(shiftId);
        }

        _logger.LogInformation("=== PHASE 1 Complete: Saving to database ===");
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("=== PHASE 2: Recalculating Nested Set values for all affected trees ===");
        await _shiftTreeService.RecalculateAllAffectedTreesAsync(processedShifts);

        _logger.LogInformation("=== PHASE 2 Complete: Saving recalculated values ===");
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("=== ProcessBatchCuts END - Processed {Count} operations ===", processedShifts.Count);
        return processedShifts;
    }

    public async Task<List<Shift>> ResetCutsAsync(Guid originalId, DateOnly newStartDate)
    {
        _logger.LogInformation("=== ResetCuts START ===");
        _logger.LogInformation("OriginalId: {OriginalId}, NewStartDate: {NewStartDate}",
            originalId, newStartDate);

        var sealedOrder = await _shiftRepository.GetSealedOrder(originalId);

        if (sealedOrder == null || !await _shiftVisibilityGuard.IsVisibleAsync(sealedOrder))
        {
            throw new InvalidOperationException(string.Format(ShiftVisibilityMessages.NoSealedOrderFound, originalId));
        }

        EnsureNotInScenario(sealedOrder, "reset cuts on");

        _logger.LogInformation("Using SealedOrder as template: ID={Id}", sealedOrder.Id);

        var allShifts = await _shiftRepository.CutList(originalId, newStartDate, tracked: true);

        _logger.LogInformation("Found {Count} shifts (filtered for cuts before {NewStartDate})",
            allShifts.Count, newStartDate);

        if (!allShifts.Any())
        {
            _logger.LogInformation("No shifts to update, returning empty list");
            return new List<Shift>();
        }

        var closeDateObj = newStartDate.AddDays(-1);
        _shiftResetService.CloseExistingSplitShifts(allShifts, closeDateObj);

        await _shiftResetService.CreateNewOriginalShiftFromSealedOrderAsync(sealedOrder, newStartDate);

        await _unitOfWork.CompleteAsync();

        var updatedShifts = await _shiftRepository.CutList(originalId);

        _logger.LogInformation("=== ResetCuts END - Returned {Count} shifts ===", updatedShifts.Count);
        return updatedShifts;
    }

    /// <summary>
    /// Refuses the whole batch for a group-restricted caller before anything is validated or written when it touches a
    /// shift they may not see: an updated shift, a stored parent of a new piece or a referenced sealed order. Each is
    /// answered with the error the later step raises for a missing one. Changed group assignments must be visible too:
    /// an update is compared with the stored shift, a new piece with its parent (the piece normally copies its groups).
    /// Parents and orders created in the same batch are checked as their own operation. Every operation is checked with
    /// its own groups, and a piece whose parent occurs several times in the batch is compared with each occurrence, so a
    /// repeated id cannot slip a hidden group past the check.
    /// </summary>
    /// <param name="operations">The batch as sent</param>
    private async Task EnsureOperationsVisibleAsync(List<CutOperation> operations)
    {
        var batchIds = operations.Select(op => op.Data.Id).ToHashSet();
        var requestedGroupsByOperation = operations.ToDictionary(op => op, op => _scheduleMapper.ToShiftEntity(op.Data).GroupItems);
        var requestedGroupsById = operations
            .GroupBy(op => op.Data.Id)
            .ToDictionary(g => g.Key, g => g.Select(op => requestedGroupsByOperation[op]).ToList());

        foreach (var op in operations)
        {
            var requestedGroups = requestedGroupsByOperation[op];

            if (op.Type == "UPDATE" && op.Data.Id != Guid.Empty)
            {
                if (!await _shiftVisibilityGuard.IsVisibleAsync(op.Data.Id))
                {
                    throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, op.Data.Id));
                }

                await _shiftVisibilityGuard.EnsureGroupChangesVisibleAsync(op.Data.Id, requestedGroups);
            }
            else if (op.Type == "CREATE" && Guid.TryParse(op.ParentId, out var parentId))
            {
                if (requestedGroupsById.TryGetValue(parentId, out var parentGroupsInBatch))
                {
                    foreach (var parentGroups in parentGroupsInBatch)
                    {
                        await _shiftVisibilityGuard.EnsureGroupChangesVisibleAsync(parentGroups, requestedGroups);
                    }
                }
                else
                {
                    if (!await _shiftVisibilityGuard.IsVisibleAsync(parentId))
                    {
                        throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ParentShiftNotFound, parentId));
                    }

                    await _shiftVisibilityGuard.EnsureGroupChangesVisibleAsync(parentId, requestedGroups);
                }
            }

            if (op.Data.OriginalId.HasValue
                && !batchIds.Contains(op.Data.OriginalId.Value)
                && !await _shiftVisibilityGuard.IsVisibleAsync(op.Data.OriginalId.Value))
            {
                throw new InvalidOperationException(
                    string.Format(ShiftVisibilityMessages.SealedOrderNotFound, op.Data.OriginalId.Value));
            }
        }
    }

    private async Task ValidateCutDates(List<CutOperation> operations)
    {
        _logger.LogInformation("=== Validating cut dates against allowed range ===");
        foreach (var op in operations)
        {
            if (op.Data.OriginalId.HasValue)
            {
                await _shiftValidator.ValidateCutDatesWithinAllowedRange(
                    op.Data.OriginalId.Value,
                    op.Data.FromDate,
                    op.Data.UntilDate,
                    _shiftRepository);

                _logger.LogInformation(
                    "Validated cut dates for OriginalId={OriginalId}, FromDate={FromDate}, UntilDate={UntilDate}",
                    op.Data.OriginalId.Value,
                    op.Data.FromDate,
                    op.Data.UntilDate);
            }
        }
    }

    private async Task<Shift> ProcessUpdate(
        CutOperation op,
        HashSet<Guid> shiftsToDelete,
        List<Shift> processedShifts)
    {
        if (op.Data.Id == Guid.Empty)
        {
            throw new InvalidOperationException("UPDATE operation requires Data.Id (Frontend must set GUID!)");
        }

        var existingShift = await _shiftRepository.Get(op.Data.Id);
        if (existingShift == null)
        {
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, op.Data.Id));
        }

        EnsureNotInScenario(existingShift, "cut/update");

        _logger.LogInformation("Existing shift loaded: ID={Id}, Status={Status}",
            existingShift.Id, existingShift.Status);

        var shift = _scheduleMapper.ToShiftEntity(op.Data);
        shift.Id = op.Data.Id;

        if (shift.Status == ShiftStatus.SplitShift &&
            (existingShift.Status == ShiftStatus.OriginalShift || existingShift.Status == ShiftStatus.SealedOrder))
        {
            _logger.LogInformation("Status change detected: {OldStatus} -> SplitShift", existingShift.Status);

            var parentId = ResolveParentId(op.ParentId);
            var parentShift = await _shiftRepository.GetTrackedOrFromDb(parentId);

            _shiftTreeService.SetHierarchyRelation(shift, null, null);
        }

        var updatedShift = await _shiftRepository.PutCutUpdate(shift);

        if (updatedShift != null)
        {
            _logger.LogInformation("Shift updated: ID={Id}, ParentId={ParentId}, RootId={RootId}",
                updatedShift.Id, updatedShift.ParentId, updatedShift.RootId);

            return updatedShift;
        }

        throw new InvalidOperationException($"Failed to update shift {op.Data.Id}");
    }

    private async Task<Shift> ProcessCreate(
        CutOperation op,
        Dictionary<Guid, List<ShiftRequiredQualification>> requirementsOfCreated)
    {
        if (op.Data.Id == Guid.Empty)
        {
            throw new InvalidOperationException("CREATE operation requires Data.Id (Frontend must generate GUID!)");
        }

        var parentId = ResolveParentId(op.ParentId);

        _logger.LogInformation("Resolved ParentId: {ParentId}", parentId);

        var parentShift = await _shiftRepository.GetTrackedOrFromDb(parentId);
        if (parentShift == null)
        {
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ParentShiftNotFound, parentId));
        }

        EnsureNotInScenario(parentShift, "cut (create child of)");

        _logger.LogInformation("Parent shift loaded: ID={Id}, Status={Status}",
            parentShift.Id, parentShift.Status);

        var shift = _scheduleMapper.ToShiftEntity(op.Data);
        shift.Id = op.Data.Id;
        shift.Status = ShiftStatus.SplitShift;

        if (parentShift.Status == ShiftStatus.SealedOrder || parentShift.Status == ShiftStatus.OriginalShift)
        {
            _logger.LogInformation("Parent is SealedOrder/OriginalShift, setting as EBENE 0 root node");
            _shiftTreeService.SetHierarchyRelation(shift, null, null);
        }
        else if (parentShift.Status == ShiftStatus.SplitShift)
        {
            _logger.LogInformation("Parent is SplitShift, setting as child node");
            _shiftTreeService.SetHierarchyRelation(shift, parentShift.Id, parentShift.RootId);
        }
        else
        {
            throw new InvalidOperationException(
                $"Cannot cut shift with status {parentShift.Status}");
        }

        if (shift.RequiredQualifications.Count == 0)
        {
            shift.RequiredQualifications = await RequirementsForNewPieceAsync(parentShift.Id, shift.Id, requirementsOfCreated);
        }

        requirementsOfCreated[shift.Id] = shift.RequiredQualifications;

        _logger.LogInformation("Adding shift to repository: ID={Id}", shift.Id);
        await _shiftRepository.Add(shift);

        _logger.LogInformation("Created shift: ID={Id}, ParentId={ParentId}, RootId={RootId}",
            shift.Id, shift.ParentId, shift.RootId);

        return shift;
    }

    /// <summary>
    /// The requirement rows a new cut piece starts with: a copy of what applied to its parent, so the piece shows its
    /// requirements in the UI and a later first row on the piece cannot silently replace them. A parent created earlier
    /// in the same batch is not saved yet, so its rows come from this batch; a stored parent's rows are resolved with
    /// the nearest-wins rule (its own rows, else those of its cut ancestors, plannable copy or sealed order).
    /// Consequence: the copy is taken at cut time, a later change on the parent does not reach existing pieces.
    /// </summary>
    /// <param name="parentId">Shift the piece is cut from</param>
    /// <param name="pieceId">The new piece</param>
    /// <param name="requirementsOfCreated">Rows of the pieces created so far in this batch; a resolved stored parent is cached here too, so a batch resolves each parent once</param>
    private async Task<List<ShiftRequiredQualification>> RequirementsForNewPieceAsync(
        Guid parentId,
        Guid pieceId,
        Dictionary<Guid, List<ShiftRequiredQualification>> requirementsOfCreated)
    {
        if (requirementsOfCreated.TryGetValue(parentId, out var batchRows))
        {
            return ShiftRequirementMaterializer.CopyAll(batchRows, pieceId);
        }

        var effective = await _requirementRepository.GetEffectiveByShiftIdsAsync([parentId]);
        var parentRows = effective.Where(entry => entry.ShiftId == parentId).Select(entry => entry.Requirement).ToList();
        requirementsOfCreated[parentId] = parentRows;
        return ShiftRequirementMaterializer.CopyAll(parentRows, pieceId);
    }

    private Guid ResolveParentId(string parentId)
    {
        if (Guid.TryParse(parentId, out var guid))
        {
            _logger.LogInformation("Parent ID: {ParentId}", guid);
            return guid;
        }

        throw new InvalidOperationException($"Invalid parent ID format: {parentId}. Frontend must send GUID!");
    }

    private static void EnsureNotInScenario(Shift shift, string operation)
    {
        if (shift.AnalyseToken != null)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} shift {shift.Id} while in scenario mode (AnalyseToken={shift.AnalyseToken}).");
        }
    }

    private List<CutOperation> TopologicalSort(List<CutOperation> operations)
    {
        _logger.LogInformation("Starting topological sort");

        var result = new List<CutOperation>();
        var visited = new HashSet<Guid>();
        var tempVisited = new HashSet<Guid>();
        var opById = new Dictionary<Guid, CutOperation>();

        foreach (var op in operations)
        {
            if (op.Data.Id == Guid.Empty)
            {
                throw new InvalidOperationException("All operations must have Data.Id set by Frontend!");
            }

            opById[op.Data.Id] = op;
        }

        void Visit(Guid id)
        {
            if (visited.Contains(id)) return;

            if (tempVisited.Contains(id))
            {
                throw new InvalidOperationException("Circular dependency detected in cut operations");
            }

            tempVisited.Add(id);

            if (opById.TryGetValue(id, out var op))
            {
                if (!string.IsNullOrEmpty(op.ParentId) && Guid.TryParse(op.ParentId, out var parentGuid))
                {
                    if (opById.ContainsKey(parentGuid))
                    {
                        Visit(parentGuid);
                    }
                }

                visited.Add(id);
                result.Add(op);
            }

            tempVisited.Remove(id);
        }

        foreach (var op in operations)
        {
            Visit(op.Data.Id);
        }

        _logger.LogInformation("Topological sort completed, sorted {Count} operations", result.Count);
        return result;
    }
}
