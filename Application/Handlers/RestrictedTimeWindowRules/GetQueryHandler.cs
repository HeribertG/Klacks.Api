// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for fetching a single restricted time window rule by Id.
/// </summary>
/// <param name="request">Contains the Id of the restricted time window rule to fetch</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.RestrictedTimeWindowRules;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<RestrictedTimeWindowRuleResource>, RestrictedTimeWindowRuleResource>
{
    private readonly IRestrictedTimeWindowRuleRepository _repository;
    private readonly ScheduleMapper _mapper;

    public GetQueryHandler(IRestrictedTimeWindowRuleRepository repository, ScheduleMapper mapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<RestrictedTimeWindowRuleResource> Handle(GetQuery<RestrictedTimeWindowRuleResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var rule = await _repository.GetAsync(request.Id);

            if (rule == null)
            {
                throw new KeyNotFoundException($"Restricted time window rule with ID {request.Id} not found");
            }

            return _mapper.ToRestrictedTimeWindowRuleResource(rule);
        }, nameof(Handle), new { request.Id });
    }
}
