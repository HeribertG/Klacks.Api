// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for the custom-rules summary: counts non-deleted scheduling rules with an empty
/// Industry (soft-deleted rows are excluded by the EF Core global query filter). Industry-bound
/// rules (from a shipped profile) never count toward this total.
/// </summary>
/// <param name="schedulingRuleRepository">Scheduling rule read access</param>

using Klacks.Api.Application.DTOs.IndustryTemplates;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.IndustryTemplates;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.IndustryTemplates;

public class CustomRulesSummaryQueryHandler : BaseHandler, IRequestHandler<CustomRulesSummaryQuery, CustomRulesSummaryResource>
{
    private readonly ISchedulingRuleRepository _schedulingRuleRepository;

    public CustomRulesSummaryQueryHandler(
        ISchedulingRuleRepository schedulingRuleRepository,
        ILogger<CustomRulesSummaryQueryHandler> logger)
        : base(logger)
    {
        _schedulingRuleRepository = schedulingRuleRepository;
    }

    public async Task<CustomRulesSummaryResource> Handle(CustomRulesSummaryQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var count = await _schedulingRuleRepository.GetCustomRuleCountAsync();
            return new CustomRulesSummaryResource { CustomSchedulingRuleCount = count };
        }, nameof(Handle));
    }
}
