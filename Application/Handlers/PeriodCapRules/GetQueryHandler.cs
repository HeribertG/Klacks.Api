// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for fetching a single period cap rule by Id.
/// </summary>
/// <param name="request">Contains the Id of the period cap rule to fetch</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PeriodCapRules;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<PeriodCapRuleResource>, PeriodCapRuleResource>
{
    private readonly IPeriodCapRuleRepository _repository;
    private readonly ScheduleMapper _mapper;

    public GetQueryHandler(IPeriodCapRuleRepository repository, ScheduleMapper mapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<PeriodCapRuleResource> Handle(GetQuery<PeriodCapRuleResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var rule = await _repository.GetAsync(request.Id);

            if (rule == null)
            {
                throw new KeyNotFoundException($"Period cap rule with ID {request.Id} not found");
            }

            return _mapper.ToPeriodCapRuleResource(rule);
        }, nameof(Handle), new { request.Id });
    }
}
