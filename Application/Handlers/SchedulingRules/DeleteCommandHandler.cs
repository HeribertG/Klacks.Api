// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for deleting a scheduling rule. After the delete is committed it raises a
/// SchedulingRuleChangedEvent so persisted work surcharges of contracts that referenced the rule
/// are recalculated against the now rule-less effective configuration.
/// </summary>
/// <param name="request">Contains the id of the scheduling rule to delete</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Events;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<SchedulingRuleResource>, SchedulingRuleResource?>
{
    private readonly ISchedulingRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public DeleteCommandHandler(
        ISchedulingRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<SchedulingRuleResource?> Handle(DeleteCommand<SchedulingRuleResource> request, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(async () =>
        {
            var existing = await _repository.Get(request.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"Scheduling rule with ID {request.Id} not found.");
            }

            var resource = _mapper.ToSchedulingRuleResource(existing);
            await _repository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return resource;
        },
        "deleting scheduling rule",
        new { RuleId = request.Id });

        if (result != null)
        {
            await DispatchRuleChangedAsync(request.Id);
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
                "Post-commit dispatch of {EventName} failed for scheduling rule {RuleId}; the delete is persisted and remains unaffected.",
                nameof(SchedulingRuleChangedEvent),
                ruleId);
        }
    }
}
