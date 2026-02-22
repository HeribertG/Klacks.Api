// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<SchedulingRuleResource>, SchedulingRuleResource>
{
    private readonly ISchedulingRuleRepository _repository;
    private readonly ScheduleMapper _mapper;

    public GetQueryHandler(ISchedulingRuleRepository repository, ScheduleMapper mapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<SchedulingRuleResource> Handle(GetQuery<SchedulingRuleResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var rule = await _repository.Get(request.Id);

            if (rule == null)
            {
                throw new KeyNotFoundException($"Scheduling rule with ID {request.Id} not found");
            }

            return _mapper.ToSchedulingRuleResource(rule);
        }, nameof(Handle), new { request.Id });
    }
}
