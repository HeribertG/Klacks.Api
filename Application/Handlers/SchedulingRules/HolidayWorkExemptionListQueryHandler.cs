// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the holiday-work exemptions an admin can see and manage.
/// </summary>
/// <param name="repository">Reads the active exemptions</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.SchedulingRules;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class HolidayWorkExemptionListQueryHandler
    : BaseHandler, IRequestHandler<HolidayWorkExemptionListQuery, List<HolidayWorkExemptionResource>>
{
    private readonly IHolidayWorkExemptionRuleRepository _repository;

    public HolidayWorkExemptionListQueryHandler(
        IHolidayWorkExemptionRuleRepository repository,
        ILogger<HolidayWorkExemptionListQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
    }

    public async Task<List<HolidayWorkExemptionResource>> Handle(
        HolidayWorkExemptionListQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var rules = await _repository.GetAllActiveAsync();
            return rules.Select(HolidayWorkExemptionMapper.ToResource).OrderBy(r => r.Description).ToList();
        },
        "loading holiday work exemptions",
        new { });
    }
}
