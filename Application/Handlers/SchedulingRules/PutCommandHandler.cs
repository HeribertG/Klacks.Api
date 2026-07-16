// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating a scheduling rule. After the update is committed it raises a
/// SchedulingRuleChangedEvent when surcharge-relevant fields (rates, night window, overtime
/// threshold) actually changed, so persisted work surcharges of referencing contracts are
/// recalculated. Planning-only fields (name, day limits, weekday flags) never trigger it.
/// </summary>
/// <param name="request">Contains the scheduling rule resource with the new values</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<SchedulingRuleResource>, SchedulingRuleResource?>
{
    private readonly ISchedulingRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PutCommandHandler(
        ISchedulingRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<SchedulingRuleResource?> Handle(PutCommand<SchedulingRuleResource> request, CancellationToken cancellationToken)
    {
        Validate(request.Resource);

        SurchargeSnapshot? before = null;
        SurchargeSnapshot? after = null;

        var result = await ExecuteAsync(async () =>
        {
            var existing = await _repository.Get(request.Resource.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Scheduling rule with ID {request.Resource.Id} not found.");
            }

            before = SurchargeSnapshot.From(existing);
            _mapper.UpdateSchedulingRuleEntity(existing, request.Resource);
            after = SurchargeSnapshot.From(existing);
            await _unitOfWork.CompleteAsync();

            return _mapper.ToSchedulingRuleResource(existing);
        },
        "updating scheduling rule",
        new { RuleId = request.Resource.Id, request.Resource.Name });

        if (result != null && before != null && after != null && before != after)
        {
            await DispatchRuleChangedAsync(result.Id);
        }

        return result;
    }

    private async Task DispatchRuleChangedAsync(Guid ruleId)
    {
        try
        {
            await _eventDispatcher.DispatchAsync(new SchedulingRuleChangedEvent(ruleId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit dispatch of {EventName} failed for scheduling rule {RuleId}; the rule update is persisted and remains unaffected.",
                nameof(SchedulingRuleChangedEvent),
                ruleId);
        }
    }

    private static void Validate(SchedulingRuleResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Scheduling rule data is required.");
        }

        if (string.IsNullOrWhiteSpace(resource.Name))
        {
            throw new InvalidRequestException("Name is required.");
        }
    }

    private sealed record SurchargeSnapshot(
        decimal? NightRate,
        decimal? HolidayRate,
        decimal? WE1Rate,
        decimal? WE2Rate,
        decimal? WE3Rate,
        string? NightStart,
        string? NightEnd,
        decimal? OvertimeThreshold)
    {
        public static SurchargeSnapshot From(SchedulingRule rule) => new(
            rule.NightRate,
            rule.HolidayRate,
            rule.WE1Rate,
            rule.WE2Rate,
            rule.WE3Rate,
            rule.NightStart,
            rule.NightEnd,
            rule.OvertimeThreshold);
    }
}
