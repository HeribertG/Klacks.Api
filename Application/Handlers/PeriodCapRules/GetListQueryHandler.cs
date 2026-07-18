// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for listing all active (non-deleted) period cap rules.
/// </summary>
/// <param name="request">Takes no parameters</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PeriodCapRules;

public class GetListQueryHandler : IRequestHandler<ListQuery<PeriodCapRuleResource>, IEnumerable<PeriodCapRuleResource>>
{
    private readonly IPeriodCapRuleRepository _repository;
    private readonly ScheduleMapper _mapper;
    private readonly ILogger<GetListQueryHandler> _logger;

    public GetListQueryHandler(IPeriodCapRuleRepository repository, ScheduleMapper mapper, ILogger<GetListQueryHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<PeriodCapRuleResource>> Handle(ListQuery<PeriodCapRuleResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var rules = await _repository.GetAllActiveAsync();
            return rules.Select(r => _mapper.ToPeriodCapRuleResource(r)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve period cap rules");
            throw new InvalidRequestException($"Failed to retrieve period cap rules: {ex.Message}");
        }
    }
}
