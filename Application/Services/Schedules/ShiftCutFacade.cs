// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class ShiftCutFacade : IShiftCutFacade
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftTreeService _shiftTreeService;
    private readonly IShiftResetService _shiftResetService;
    private readonly IShiftValidator _shiftValidator;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ShiftCutFacade> _logger;

    public ShiftCutFacade(
        IShiftRepository shiftRepository,
        IShiftTreeService shiftTreeService,
        IShiftResetService shiftResetService,
        IShiftValidator shiftValidator,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<ShiftCutFacade> logger)
    {
        _shiftRepository = shiftRepository;
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

        await ValidateCutDates(operations);

        var processedShifts = new List<Shift>();
        var shiftsToDelete = new HashSet<Guid>();

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
                var createdShift = await ProcessCreate(op, shiftsToDelete, processedShifts);
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

        if (sealedOrder == null)
        {
            throw new InvalidOperationException($"No SealedOrder found for OriginalId {originalId}");
        }

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
            throw new KeyNotFoundException($"Shift with ID {op.Data.Id} not found");
        }

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

        var updatedShift = await _shiftRepository.Put(shift);

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
        HashSet<Guid> shiftsToDelete,
        List<Shift> processedShifts)
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
            throw new KeyNotFoundException($"Parent shift with ID {parentId} not found");
        }

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

        _logger.LogInformation("Adding shift to repository: ID={Id}", shift.Id);
        await _shiftRepository.Add(shift);

        _logger.LogInformation("Created shift: ID={Id}, ParentId={ParentId}, RootId={RootId}",
            shift.Id, shift.ParentId, shift.RootId);

        return shift;
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
