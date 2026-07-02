// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles an ERP update to an already-sealed order: closes the old SealedOrder (UntilDate, never
/// deleted -- history and already-worked Work stay correct), cancels its future/not-yet-locked
/// Work, notifies planners per cancellation, and opens a fresh OriginalOrder draft carrying the
/// same ExternalOrderReference via SupersedesOrderId. A no-op when the new payload does not
/// actually differ from the sealed data, so a full nightly ERP extract does not re-trigger this
/// on every unchanged order.
/// </summary>
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Imports;

public class OrderSupersessionService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IAgentTriggerService _triggerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderSupersessionService> _logger;

    public OrderSupersessionService(
        IShiftRepository shiftRepository,
        IWorkRepository workRepository,
        IClientRepository clientRepository,
        IAgentTriggerService triggerService,
        IUnitOfWork unitOfWork,
        ILogger<OrderSupersessionService> logger)
    {
        _shiftRepository = shiftRepository;
        _workRepository = workRepository;
        _clientRepository = clientRepository;
        _triggerService = triggerService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(Shift sealedOrder, ImportedOrderPayload order, Guid clientId, CancellationToken cancellationToken = default)
    {
        if (!ImportedOrderShiftMapper.DiffersFromSealedOrder(sealedOrder, order, clientId))
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var droppedWork = new List<Work>();

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            sealedOrder.UntilDate = today;
            await _shiftRepository.PutWithSealedOrderHandling(sealedOrder);

            var derivedShifts = await _shiftRepository.CutList(sealedOrder.Id);
            var shiftIds = derivedShifts.Select(s => s.Id).ToList();
            var futureWork = await _workRepository.GetFutureUnlockedByShiftIdsAsync(shiftIds, today, cancellationToken);

            foreach (var work in futureWork)
            {
                await _workRepository.Delete(work.Id);
                droppedWork.Add(work);
            }

            var newDraft = ImportedOrderShiftMapper.BuildDraft(order, clientId);
            newDraft.SupersedesOrderId = sealedOrder.Id;
            await _shiftRepository.AddWithSealedOrderHandling(newDraft);

            await _unitOfWork.CompleteAsync();
            return true;
        });

        _logger.LogInformation(
            "ERP import: superseded order {Reference}, closed {ClosedId}, dropped {Count} future work entr(y/ies)",
            order.ExternalOrderReference, sealedOrder.Id, droppedWork.Count);

        foreach (var work in droppedWork)
        {
            var employee = await _clientRepository.GetNoTracking(work.ClientId);
            var employeeName = employee != null ? $"{employee.FirstName} {employee.Name}".Trim() : work.ClientId.ToString();
            await _triggerService.OnEventAsync(new WorkDroppedByErpImportTriggerEvent(work.Id, employeeName, work.CurrentDate), cancellationToken);
        }
    }
}
