// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating a contract. After the update is committed it raises a ContractChangedEvent
/// when calculation-relevant fields (rates, night window, validity dates, scheduling rule or calendar
/// binding, payment interval, hour bounds) actually changed, so persisted work surcharges in the
/// affected window are recalculated. Name and weekday planning flags never trigger a recalculation.
/// </summary>
/// <param name="request">Contains the contract resource with the new values</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.Contracts;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ContractResource>, ContractResource?>
{
    private readonly IContractRepository _contractRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PutCommandHandler(
        IContractRepository contractRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _contractRepository = contractRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<ContractResource?> Handle(PutCommand<ContractResource> request, CancellationToken cancellationToken)
    {
        ValidateContractRequest(request.Resource);

        ContractCalculationSnapshot? before = null;
        ContractCalculationSnapshot? after = null;

        var result = await ExecuteAsync(async () =>
        {
            var existingContract = await _contractRepository.Get(request.Resource.Id);
            if (existingContract == null)
            {
                throw new KeyNotFoundException($"Contract with ID {request.Resource.Id} not found.");
            }

            before = ContractCalculationSnapshot.From(existingContract);
            _scheduleMapper.UpdateContractEntity(existingContract, request.Resource);
            after = ContractCalculationSnapshot.From(existingContract);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToContractResource(existingContract);
        },
        "updating contract",
        new { ContractId = request.Resource.Id, ContractName = request.Resource?.Name });

        if (result != null && before != null && after != null && before != after)
        {
            await DispatchContractChangedAsync(result.Id, before, after);
        }

        return result;
    }

    private async Task DispatchContractChangedAsync(
        Guid contractId,
        ContractCalculationSnapshot before,
        ContractCalculationSnapshot after)
    {
        try
        {
            var recalculationFrom = DateOnly.FromDateTime(before.ValidFrom < after.ValidFrom ? before.ValidFrom : after.ValidFrom);
            var domainEvent = new ContractChangedEvent(contractId, null, recalculationFrom, null);

            await _eventDispatcher.DispatchAsync(domainEvent, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit dispatch of {EventName} failed for contract {ContractId}; the contract update is persisted and remains unaffected.",
                nameof(ContractChangedEvent),
                contractId);
        }
    }

    private void ValidateContractRequest(ContractResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Contract data is required.");
        }

        if (string.IsNullOrWhiteSpace(resource.Name))
        {
            throw new InvalidRequestException("Contract name is required.");
        }

        if (resource.MinimumHours > resource.MaximumHours)
        {
            throw new InvalidRequestException("Minimum hours cannot exceed maximum hours.");
        }

        if (resource.GuaranteedHours.HasValue &&
            (resource.GuaranteedHours < resource.MinimumHours ||
             resource.GuaranteedHours > resource.MaximumHours))
        {
            throw new InvalidRequestException("Guaranteed hours must be between minimum and maximum hours.");
        }

        if (resource.ValidUntil.HasValue && resource.ValidUntil <= resource.ValidFrom)
        {
            throw new InvalidRequestException("Valid until date must be after valid from date.");
        }
    }

    private sealed record ContractCalculationSnapshot(
        decimal? GuaranteedHours,
        decimal? MaximumHours,
        decimal? MinimumHours,
        decimal? FullTime,
        decimal? NightRate,
        decimal? HolidayRate,
        decimal? WE1Rate,
        decimal? WE2Rate,
        decimal? WE3Rate,
        string? NightStart,
        string? NightEnd,
        PaymentInterval PaymentInterval,
        DateTime ValidFrom,
        DateTime? ValidUntil,
        Guid? CalendarSelectionId,
        Guid? SchedulingRuleId)
    {
        public static ContractCalculationSnapshot From(Contract contract) => new(
            contract.GuaranteedHours,
            contract.MaximumHours,
            contract.MinimumHours,
            contract.FullTime,
            contract.NightRate,
            contract.HolidayRate,
            contract.WE1Rate,
            contract.WE2Rate,
            contract.WE3Rate,
            contract.NightStart,
            contract.NightEnd,
            contract.PaymentInterval,
            contract.ValidFrom,
            contract.ValidUntil,
            contract.CalendarSelectionId,
            contract.SchedulingRuleId);
    }
}
