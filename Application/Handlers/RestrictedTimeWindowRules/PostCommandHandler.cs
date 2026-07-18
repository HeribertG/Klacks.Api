// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating a customer-facing restricted time window rule. New rows always get an empty
/// ImportSourceKey/ImportContentHash so they are never touched by the region-setup re-import.
/// </summary>
/// <param name="request">Contains the restricted time window rule resource with the season tuple, the
/// daily forbidden window and the AppliesToGroupTag scope</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.RestrictedTimeWindowRules;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<RestrictedTimeWindowRuleResource>, RestrictedTimeWindowRuleResource?>
{
    private readonly IRestrictedTimeWindowRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IRestrictedTimeWindowRuleRepository repository,
        ScheduleMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<RestrictedTimeWindowRuleResource?> Handle(PostCommand<RestrictedTimeWindowRuleResource> request, CancellationToken cancellationToken)
    {
        RestrictedTimeWindowRuleValidation.Validate(request.Resource);

        return await ExecuteAsync(async () =>
        {
            var entity = _mapper.ToRestrictedTimeWindowRuleEntity(request.Resource!);
            entity.Id = Guid.NewGuid();
            entity.ImportSourceKey = string.Empty;
            entity.ImportContentHash = string.Empty;

            _repository.Add(entity);
            await _unitOfWork.CompleteAsync();
            return _mapper.ToRestrictedTimeWindowRuleResource(entity);
        },
        "creating restricted time window rule",
        new { request.Resource?.SeasonFromMonth, request.Resource?.SeasonToMonth, request.Resource?.AppliesToGroupTag });
    }
}
