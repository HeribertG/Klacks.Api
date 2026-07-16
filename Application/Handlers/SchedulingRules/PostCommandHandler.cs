// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating a scheduling rule. After the create is committed it raises a
/// SchedulingRuleChangedEvent; for a freshly created rule no contract references it yet, so the
/// recalculation handler is a cheap no-op, but the dispatch keeps all rule mutations symmetric.
/// </summary>
/// <param name="request">Contains the scheduling rule resource to create</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<SchedulingRuleResource>, SchedulingRuleResource?>
{
    private readonly ISchedulingRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public PostCommandHandler(
        ISchedulingRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<SchedulingRuleResource?> Handle(PostCommand<SchedulingRuleResource> request, CancellationToken cancellationToken)
    {
        Validate(request.Resource);

        var result = await ExecuteAsync(async () =>
        {
            var entity = _mapper.ToSchedulingRuleEntity(request.Resource);
            await _repository.Add(entity);
            await _unitOfWork.CompleteAsync();
            return _mapper.ToSchedulingRuleResource(entity);
        },
        "creating scheduling rule",
        new { request.Resource.Name });

        if (result != null)
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
                "Post-commit dispatch of {EventName} failed for scheduling rule {RuleId}; the created rule is persisted and remains unaffected.",
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
}
