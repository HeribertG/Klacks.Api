// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for listing all active (non-deleted) restricted time window rules.
/// </summary>
/// <param name="request">Takes no parameters</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.RestrictedTimeWindowRules;

public class GetListQueryHandler : IRequestHandler<ListQuery<RestrictedTimeWindowRuleResource>, IEnumerable<RestrictedTimeWindowRuleResource>>
{
    private readonly IRestrictedTimeWindowRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly ILogger<GetListQueryHandler> _logger;

    public GetListQueryHandler(IRestrictedTimeWindowRuleRepository repository, ScheduleMapper mapper, ILogger<GetListQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<RestrictedTimeWindowRuleResource>> Handle(ListQuery<RestrictedTimeWindowRuleResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _repository.GetAllActiveAsync();
            return rules.Select(r => _mapper.ToRestrictedTimeWindowRuleResource(r)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve restricted time window rules");
            throw new InvalidRequestException($"Failed to retrieve restricted time window rules: {ex.Message}");
        }
    }
}
