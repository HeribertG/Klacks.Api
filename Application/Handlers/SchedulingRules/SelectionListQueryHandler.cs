// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for the scheduling-rule selection list. Applies ACTIVE_INDUSTRIES filtering so choice
/// lists only offer rules of active industries plus industry-less rules; without the setting the
/// full list is returned unfiltered.
/// </summary>
/// <param name="repository">Scheduling rule read access</param>
/// <param name="activeIndustriesProvider">Resolves the active industry slugs from settings</param>
/// <param name="mapper">Maps scheduling rule entities to resources</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.SchedulingRules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class SelectionListQueryHandler : BaseHandler, IRequestHandler<SelectionListQuery, IEnumerable<SchedulingRuleResource>>
{
    private readonly ISchedulingRuleRepository _repository;
    private readonly IActiveIndustriesProvider _activeIndustriesProvider;
    private readonly ScheduleMapper _mapper;

    public SelectionListQueryHandler(
        ISchedulingRuleRepository repository,
        IActiveIndustriesProvider activeIndustriesProvider,
        ScheduleMapper mapper,
        ILogger<SelectionListQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _activeIndustriesProvider = activeIndustriesProvider;
        _mapper = mapper;
    }

    public async Task<IEnumerable<SchedulingRuleResource>> Handle(SelectionListQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var activeIndustrySlugs = await _activeIndustriesProvider.GetActiveIndustrySlugsAsync();
            var rules = activeIndustrySlugs == null
                ? await _repository.List()
                : await _repository.GetSelectableAsync(activeIndustrySlugs);
            return rules.Select(r => _mapper.ToSchedulingRuleResource(r)).ToList();
        }, nameof(Handle));
    }
}
