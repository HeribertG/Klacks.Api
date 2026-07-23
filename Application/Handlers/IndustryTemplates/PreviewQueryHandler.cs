// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for the industry template preview. Validates the requested slug against
/// IndustrySlugs.All and returns every scheduling rule and qualification tagged with that
/// industry, regardless of the current ACTIVE_INDUSTRIES setting. Scheduling rules are mapped
/// with their concrete planning values (hours caps, overtime tiers, night/holiday/weekend rates)
/// so an admin can see what a profile actually configures before activating it.
/// </summary>
/// <param name="schedulingRuleRepository">Scheduling rule read access</param>
/// <param name="qualificationRepository">Qualification read access</param>

using Klacks.Api.Application.DTOs.IndustryTemplates;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.IndustryTemplates;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.IndustryTemplates;

public class PreviewQueryHandler : BaseHandler, IRequestHandler<PreviewQuery, IndustryTemplatePreviewResource>
{
    private readonly ISchedulingRuleRepository _schedulingRuleRepository;
    private readonly IQualificationRepository _qualificationRepository;

    public PreviewQueryHandler(
        ISchedulingRuleRepository schedulingRuleRepository,
        IQualificationRepository qualificationRepository,
        ILogger<PreviewQueryHandler> logger)
        : base(logger)
    {
        _schedulingRuleRepository = schedulingRuleRepository;
        _qualificationRepository = qualificationRepository;
    }

    public async Task<IndustryTemplatePreviewResource> Handle(PreviewQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var slug = request.Industry.Trim().ToLowerInvariant();
            if (!IndustrySlugs.All.Contains(slug))
            {
                throw new InvalidRequestException($"Industry template preview: unknown industry slug '{request.Industry}'.");
            }

            var rules = await _schedulingRuleRepository.GetByIndustryAsync(slug);
            var qualifications = await _qualificationRepository.GetByIndustryAsync(slug, cancellationToken);

            return new IndustryTemplatePreviewResource
            {
                Industry = slug,
                SchedulingRules = rules.Select(ToSchedulingRuleItem).ToList(),
                Qualifications = qualifications
                    .Select(qualification => new IndustryTemplateQualificationItem { Name = qualification.Name, Description = qualification.Description })
                    .ToList(),
            };
        }, nameof(Handle));
    }

    private static IndustryTemplateSchedulingRuleItem ToSchedulingRuleItem(SchedulingRule rule)
    {
        return new IndustryTemplateSchedulingRuleItem
        {
            Name = rule.Name,
            Description = null,
            DefaultWorkingHours = rule.DefaultWorkingHours,
            FullTimeHours = rule.FullTimeHours,
            MaxDailyHours = rule.MaxDailyHours,
            MaxWeeklyHours = rule.MaxWeeklyHours,
            MaxConsecutiveDays = rule.MaxConsecutiveDays,
            MinRestDays = rule.MinRestDays,
            MinPauseHours = rule.MinPauseHours,
            OvertimeThreshold = rule.OvertimeThreshold,
            OvertimeBasis = rule.OvertimeBasis?.ToString(),
            OvertimeRateMode = rule.OvertimeRateMode?.ToString(),
            OvertimeTier1AfterHours = rule.OvertimeTier1AfterHours,
            OvertimeTier1Rate = rule.OvertimeTier1Rate,
            OvertimeTier2AfterHours = rule.OvertimeTier2AfterHours,
            OvertimeTier2Rate = rule.OvertimeTier2Rate,
            OvertimeTier3AfterHours = rule.OvertimeTier3AfterHours,
            OvertimeTier3Rate = rule.OvertimeTier3Rate,
            NightStart = rule.NightStart,
            NightEnd = rule.NightEnd,
            NightRate = rule.NightRate,
            HolidayRate = rule.HolidayRate,
            Weekend1Rate = rule.WE1Rate,
            Weekend2Rate = rule.WE2Rate,
            Weekend3Rate = rule.WE3Rate,
            VacationDaysPerYear = rule.VacationDaysPerYear,
            MaxWorkDays = rule.MaxWorkDays,
            MaxOptimalGap = rule.MaxOptimalGap,
            GuaranteedHours = rule.GuaranteedHours,
            MaximumHours = rule.MaximumHours,
            MinimumHours = rule.MinimumHours,
        };
    }
}
