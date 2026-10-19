// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Imports one parsed ERP order and reports what it did. Resolves (or creates) the customer, then looks up
/// the active shift for the order's external reference: none -> a new unsealed draft (Created); a
/// SealedOrder -> OrderSupersessionService, which supersedes it when the payload changed (Superseded) or
/// leaves it alone (Unchanged); an existing draft -> overwritten only when DiffersFromDraft reports a
/// change (Updated), otherwise nothing is written (Unchanged), so a full nightly re-delivery does not touch
/// every draft. Every write runs in its own transaction; exceptions propagate to the caller, which records
/// the order as rejected.
/// </summary>
/// <param name="customerResolver">Finds or creates the order's customer client</param>
/// <param name="shiftRepository">Finds the active shift for the external reference and writes drafts</param>
/// <param name="supersessionService">Handles a re-delivery of an already sealed order (own transaction)</param>
/// <param name="unitOfWork">Wraps each write in a transaction</param>
/// <param name="defaultShiftMacroResolver">Supplies the default Shift-category macro for a newly created draft; an
/// overwritten draft keeps its macro, so a planner's choice survives every re-delivery</param>
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;

namespace Klacks.Api.Application.Services.Imports;

public class ImportedOrderProcessor
{
    private readonly ErpCustomerResolver _customerResolver;
    private readonly IShiftRepository _shiftRepository;
    private readonly OrderSupersessionService _supersessionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDefaultShiftMacroResolver _defaultShiftMacroResolver;

    public ImportedOrderProcessor(
        ErpCustomerResolver customerResolver,
        IShiftRepository shiftRepository,
        OrderSupersessionService supersessionService,
        IUnitOfWork unitOfWork,
        IDefaultShiftMacroResolver defaultShiftMacroResolver)
    {
        _customerResolver = customerResolver;
        _shiftRepository = shiftRepository;
        _supersessionService = supersessionService;
        _unitOfWork = unitOfWork;
        _defaultShiftMacroResolver = defaultShiftMacroResolver;
    }

    public async Task<ImportedOrderOutcome> ProcessOrderAsync(ImportedOrderPayload order, CancellationToken cancellationToken = default)
    {
        var client = await _unitOfWork.ExecuteInTransactionAsync(() => _customerResolver.ResolveAsync(order.Customer, cancellationToken));

        var existing = await _shiftRepository.FindActiveByExternalReferenceAsync(order.SourceSystemId, order.ExternalOrderReference, cancellationToken);

        if (existing == null)
        {
            var draft = ImportedOrderShiftMapper.BuildDraft(order, client.Id);
            draft.MacroId = await _defaultShiftMacroResolver.ResolveDefaultMacroIdAsync(cancellationToken);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _shiftRepository.AddWithSealedOrderHandling(draft);
                await _unitOfWork.CompleteAsync();
                return true;
            });
            return ImportedOrderOutcome.Created;
        }

        if (existing.Status == ShiftStatus.SealedOrder)
        {
            // OrderSupersessionService manages its own transaction -- must not nest inside another.
            var superseded = await _supersessionService.HandleAsync(existing, order, client.Id, cancellationToken);
            return superseded ? ImportedOrderOutcome.Superseded : ImportedOrderOutcome.Unchanged;
        }

        if (!ImportedOrderShiftMapper.DiffersFromDraft(existing, order, client.Id))
        {
            return ImportedOrderOutcome.Unchanged;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ImportedOrderShiftMapper.ApplyToDraft(existing, order, client.Id);
            await _shiftRepository.PutWithSealedOrderHandling(existing);
            await _unitOfWork.CompleteAsync();
            return true;
        });
        return ImportedOrderOutcome.Updated;
    }
}
