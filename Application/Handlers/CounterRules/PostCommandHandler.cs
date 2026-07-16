// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating a customer-facing counter rule. New rows always get an empty
/// ImportSourceKey/ImportContentHash so they are never touched by the region-setup re-import.
/// </summary>
/// <param name="request">Contains the counter rule resource with EventType, Period, Threshold,
/// HoursThreshold and the optional SchedulingRuleId scope</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.CounterRules;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<CounterRuleResource>, CounterRuleResource?>
{
    private readonly ICounterRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        ICounterRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CounterRuleResource?> Handle(PostCommand<CounterRuleResource> request, CancellationToken cancellationToken)
    {
        CounterRuleValidation.Validate(request.Resource);

        return await ExecuteAsync(async () =>
        {
            var entity = _mapper.ToCounterRuleEntity(request.Resource!);
            entity.Id = Guid.NewGuid();
            entity.ImportSourceKey = string.Empty;
            entity.ImportContentHash = string.Empty;

            _repository.Add(entity);
            await _unitOfWork.CompleteAsync();
            return _mapper.ToCounterRuleResource(entity);
        },
        "creating counter rule",
        new { request.Resource?.EventType, request.Resource?.Period, request.Resource?.Threshold });
    }
}
