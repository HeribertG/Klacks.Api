// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for fetching a single counter rule by Id.
/// </summary>
/// <param name="request">Contains the Id of the counter rule to fetch</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.CounterRules;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<CounterRuleResource>, CounterRuleResource>
{
    private readonly ICounterRuleRepository _repository;
    private readonly ScheduleMapper _mapper;

    public GetQueryHandler(ICounterRuleRepository repository, ScheduleMapper mapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CounterRuleResource> Handle(GetQuery<CounterRuleResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var rule = await _repository.GetAsync(request.Id);

            if (rule == null)
            {
                throw new KeyNotFoundException($"Counter rule with ID {request.Id} not found");
            }

            return _mapper.ToCounterRuleResource(rule);
        }, nameof(Handle), new { request.Id });
    }
}
