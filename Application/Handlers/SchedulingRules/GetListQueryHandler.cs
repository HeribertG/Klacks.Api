// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class GetListQueryHandler : IRequestHandler<ListQuery<SchedulingRuleResource>, IEnumerable<SchedulingRuleResource>>
{
    private readonly ISchedulingRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly ILogger<GetListQueryHandler> _logger;

    public GetListQueryHandler(ISchedulingRuleRepository repository, ScheduleMapper mapper, ILogger<GetListQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<SchedulingRuleResource>> Handle(ListQuery<SchedulingRuleResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _repository.List();
            return rules.Select(r => _mapper.ToSchedulingRuleResource(r)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve scheduling rules");
            throw new InvalidRequestException($"Failed to retrieve scheduling rules: {ex.Message}");
        }
    }
}
