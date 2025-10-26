using AutoMapper;
using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostBatchCutsCommandHandler : BaseHandler, IRequestHandler<PostBatchCutsCommand, List<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftTreeService _shiftTreeService;
    private readonly IShiftValidator _shiftValidator;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostBatchCutsCommandHandler(
        IShiftRepository shiftRepository,
        IShiftTreeService shiftTreeService,
        IShiftValidator shiftValidator,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostBatchCutsCommandHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _shiftTreeService = shiftTreeService;
        _shiftValidator = shiftValidator;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ShiftResource>> Handle(PostBatchCutsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== PostBatchCutsCommand START (2-Phase Approach) ===");
        _logger.LogInformation("Number of operations: {OpCount}", request.Operations.Count);

        _logger.LogInformation("=== Validating cut dates against allowed range ===");
        foreach (var op in request.Operations)
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

        var results = new List<ShiftResource>();
        var shiftsToDelete = new HashSet<Guid>();
        var processedShifts = new List<Shift>();

        var sortedOps = TopologicalSort(request.Operations);

        _logger.LogInformation("=== PHASE 1: Processing operations and setting hierarchy relations ===");

        foreach (var op in sortedOps)
        {
            if (op.Type == "UPDATE")
            {
                _logger.LogInformation("Processing UPDATE for ShiftId={ShiftId}", op.Data.Id);
                var updatedShift = await ProcessUpdate(op, shiftsToDelete, processedShifts);
                results.Add(updatedShift);
            }
            else if (op.Type == "CREATE")
            {
                _logger.LogInformation("Processing CREATE with ShiftId={ShiftId}, ParentId={ParentId}",
                    op.Data.Id, op.ParentId);
                var createdShift = await ProcessCreate(op, shiftsToDelete, processedShifts);
                results.Add(createdShift);
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

        _logger.LogInformation("=== PostBatchCutsCommand END - Processed {Count} operations ===", results.Count);
        return results;
    }

    private async Task<ShiftResource> ProcessUpdate(
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

        var shift = _mapper.Map<Shift>(op.Data);
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
            processedShifts.Add(updatedShift);

            _logger.LogInformation("Shift updated: ID={Id}, ParentId={ParentId}, RootId={RootId}",
                updatedShift.Id, updatedShift.ParentId, updatedShift.RootId);

            return _mapper.Map<ShiftResource>(updatedShift);
        }

        throw new InvalidOperationException($"Failed to update shift {op.Data.Id}");
    }

    private async Task<ShiftResource> ProcessCreate(
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

        var shift = _mapper.Map<Shift>(op.Data);
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

        processedShifts.Add(shift);

        var mappedResource = _mapper.Map<ShiftResource>(shift);
        _logger.LogInformation("Created shift: ID={Id}, ParentId={ParentId}, RootId={RootId}",
            shift.Id, mappedResource.ParentId, mappedResource.RootId);

        return mappedResource;
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
