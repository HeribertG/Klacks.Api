// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports by when planning has to be finished so the plan can still be delivered in time. Without numbers it
/// reports the facts the conversation needs (today, the legal minimum publication lead, an already stored
/// deadline lead) — also when only the delivery channel or transit days were already collected, because the
/// guided dialog carries those values along before the calculation is due; with the announcement days and the review buffer (and the postal transit days when the plan
/// goes by post) it computes, per staffed group, the latest send date and the latest planning-done date of the
/// first period whose deadline can still be met, and flags a next period that is already past its deadline.
/// The compliance minimum is used as a floor for the announcement days and is never written. Read-only:
/// nothing is stored here.
/// </summary>
/// <param name="groupRepository">Lists the groups and which of them have members</param>
/// <param name="weekConfiguration">Resolves the configured week start for weekly period boundaries</param>
/// <param name="settingsReader">Reads the compliance minimum and the stored deadline lead</param>
/// <param name="companyClock">Resolves today as the company's own local day</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_planning_deadline")]
public class GetPlanningDeadlineSkill : BaseSkillImplementation
{
    private const string IsoDateFormat = "yyyy-MM-dd";
    private const string ContextMode = "Context";
    private const string ComputedMode = "Computed";
    private const string PostChannel = "post";
    private const int MaxReportedGroups = 25;
    private const string Formula =
        "planning done by = period start - announcement days - transit days - review days";

    private readonly IGroupRepository _groupRepository;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly ISettingsReader _settingsReader;
    private readonly ICompanyClock _companyClock;

    public GetPlanningDeadlineSkill(
        IGroupRepository groupRepository,
        IWeekConfiguration weekConfiguration,
        ISettingsReader settingsReader,
        ICompanyClock companyClock)
    {
        _groupRepository = groupRepository;
        _weekConfiguration = weekConfiguration;
        _settingsReader = settingsReader;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var complianceMin = await ReadPositiveIntAsync(SettingKeys.ComplianceRosterPublicationMinLeadDays);
        var storedLead = await ReadPositiveIntAsync(SettingKeys.PlanningDeadlineLeadDays);

        var announcement = GetParameter<int?>(parameters, PlanningDeadlineParameters.AnnouncementDays);
        var review = GetParameter<int?>(parameters, PlanningDeadlineParameters.ReviewDays);
        var transit = GetParameter<int?>(parameters, PlanningDeadlineParameters.TransitDays);

        if (announcement is null && review is null)
        {
            return BuildContextResult(today, complianceMin, storedLead);
        }

        var missing = new List<string>();
        if (announcement is null) missing.Add(PlanningDeadlineParameters.AnnouncementDays);
        if (review is null) missing.Add(PlanningDeadlineParameters.ReviewDays);
        if (missing.Count > 0)
        {
            return SkillResult.Error($"Missing required value(s): {string.Join(", ", missing)}.");
        }

        var channel = GetParameter<string>(parameters, PlanningDeadlineParameters.DeliveryChannel);
        if (transit is null && string.Equals(channel?.Trim(), PostChannel, StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.Error(
                $"Missing required value: {PlanningDeadlineParameters.TransitDays} (days from dispatch to arrival, needed for post).");
        }

        var transitDays = transit ?? 0;
        if (!PlanningDeadlineCalculator.IsValidDays(announcement!.Value)
            || !PlanningDeadlineCalculator.IsValidDays(transitDays)
            || !PlanningDeadlineCalculator.IsValidDays(review!.Value))
        {
            return SkillResult.Error(
                $"Every value must be a whole number of days between 0 and {PlanningDeadlineCalculator.MaxDays}.");
        }

        var effectiveAnnouncement = PlanningDeadlineCalculator.EffectiveAnnouncement(announcement.Value, complianceMin);
        var leadDays = PlanningDeadlineCalculator.LeadDays(effectiveAnnouncement, transitDays, review.Value);
        var groups = await BuildGroupRowsAsync(
            today, effectiveAnnouncement, transitDays, review.Value, cancellationToken);

        return SkillResult.SuccessResult(
            new
            {
                Mode = ComputedMode,
                Today = FormatDate(today),
                DeliveryChannel = channel,
                AnnouncementDays = effectiveAnnouncement,
                AnnouncementFloorApplied = effectiveAnnouncement > announcement.Value,
                ComplianceMinLeadDays = complianceMin,
                StoredDeadlineLeadDays = storedLead,
                TransitDays = transitDays,
                ReviewDays = review.Value,
                DeadlineLeadDays = leadDays,
                Formula,
                Groups = groups.Take(MaxReportedGroups).ToList(),
                OmittedGroups = Math.Max(0, groups.Count - MaxReportedGroups)
            },
            $"Deadline lead is {leadDays} day(s) before a period starts "
            + $"({effectiveAnnouncement} announcement + {transitDays} transit + {review.Value} review). "
            + $"{groups.Count} group(s) with a derivable next period. When a group's next period is already "
            + "past its deadline, NextPeriodOverdue is true and the listed period is the first one that can "
            + "still be met. Postal delivery is only calculated, Klacks does not send post.");
    }

    private static SkillResult BuildContextResult(DateOnly today, int complianceMin, int storedLead)
    {
        return SkillResult.SuccessResult(
            new
            {
                Mode = ContextMode,
                Today = FormatDate(today),
                ComplianceMinLeadDays = complianceMin,
                StoredDeadlineLeadDays = storedLead,
                Formula
            },
            $"Planning deadline context: today is {FormatDate(today)}, "
            + $"legal minimum publication lead {complianceMin} day(s) (0 = not configured), "
            + $"stored deadline lead {storedLead} day(s) (0 = not configured).");
    }

    private async Task<List<GroupDeadlineRow>> BuildGroupRowsAsync(
        DateOnly today, int announcementDays, int transitDays, int reviewDays, CancellationToken cancellationToken)
    {
        var groups = await _groupRepository.List();
        var staffing = GroupStaffingLookup.Build(
            groups, await _groupRepository.GetGroupIdsWithMembersAsync(cancellationToken));
        var weekStart = await _weekConfiguration.GetWeekStartAsync(today, cancellationToken);
        var nextWeekStart = weekStart.AddDays(NextPeriodBoundaries.WeeklyPeriodDays);

        return groups
            .Where(group => NextPeriodBoundaries.HasDerivableCycle(group.PaymentInterval) && staffing.IsStaffed(group.Id))
            .Select(group =>
            {
                var nextStart = NextPeriodBoundaries.ComputeStart(group, today, nextWeekStart);
                var next = PlanningDeadlineCalculator.Compute(
                    nextStart, today, announcementDays, transitDays, reviewDays);
                var reachable = PlanningDeadlineCalculator.FirstReachable(
                    end => end.AddDays(1), nextStart, today, announcementDays, transitDays, reviewDays,
                    start => NextPeriodBoundaries.ComputeEnd(group, start));

                return new GroupDeadlineRow(
                    group.Name,
                    FormatDate(reachable.PeriodStart),
                    FormatDate(reachable.SendByDate),
                    FormatDate(reachable.PlanningDoneBy),
                    reachable.DaysRemaining,
                    NextPeriodOverdue: next.DaysRemaining < 0,
                    NextPeriodStart: FormatDate(nextStart),
                    NextPeriodPlanningDoneBy: FormatDate(next.PlanningDoneBy));
            })
            .OrderBy(row => row.PlanningDoneBy, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<int> ReadPositiveIntAsync(string key)
    {
        var setting = await _settingsReader.GetSetting(key);
        if (setting?.Value != null
            && int.TryParse(setting.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            && value > 0)
        {
            return value;
        }

        return 0;
    }

    private static string FormatDate(DateOnly date) => date.ToString(IsoDateFormat, CultureInfo.InvariantCulture);

    private sealed record GroupDeadlineRow(
        string GroupName,
        string PeriodStart,
        string SendByDate,
        string PlanningDoneBy,
        int DaysRemaining,
        bool NextPeriodOverdue,
        string NextPeriodStart,
        string NextPeriodPlanningDoneBy);
}
