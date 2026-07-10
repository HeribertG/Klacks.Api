// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shows the sealing status of a billing period (the badge row of the period-closing page):
/// how many days are day-locked, how many carry only partial item seals and how many are
/// still open, plus per-day counts on request. Optionally group-scoped.
/// </summary>
/// <param name="startDate">Period start in ISO yyyy-MM-dd (inclusive).</param>
/// <param name="endDate">Period end in ISO yyyy-MM-dd (inclusive).</param>
/// <param name="groupId">Optional. UUID of the group scope; omitted = across all groups.</param>
/// <param name="groupName">Optional. Display name of the group; resolved with fuzzy matching.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_period_status")]
public class GetPeriodStatusSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;

    public GetPeriodStatusSkill(
        IMediator mediator,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard)
    {
        _mediator = mediator;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var startDate = GetParameter<DateOnly?>(parameters, "startDate")
            ?? throw new ArgumentException("Required parameter 'startDate' is missing");
        var endDate = GetParameter<DateOnly?>(parameters, "endDate")
            ?? throw new ArgumentException("Required parameter 'endDate' is missing");
        if (startDate > endDate)
        {
            return SkillResult.Error($"startDate ({startDate}) must be on or before endDate ({endDate}).");
        }

        var (groupId, groupName, groupError) = await PeriodClosingGroupResolver.ResolveAsync(
            GetParameter<string>(parameters, "groupId"),
            GetParameter<string>(parameters, "groupName"),
            _groupRepository, _groupScopeGuard, context, cancellationToken);
        if (groupError != null)
        {
            return SkillResult.Error(groupError);
        }

        var days = await _mediator.Send(
            new GetSealedPeriodsQuery(startDate, endDate, groupId), cancellationToken);

        var sealedDays = days.Count(d => d.IsDaySealed);
        var partialDays = days.Count(d => !d.IsDaySealed && d.SealedWorkCount > 0);
        var openDays = days.Count(d => !d.IsDaySealed && d.SealedWorkCount == 0 && d.TotalWorkCount > 0);
        var emptyDays = days.Count(d => d.TotalWorkCount == 0 && !d.IsDaySealed);
        var fullySealed = days.Count > 0 && days.All(d => d.IsDaySealed);

        var openDayList = days
            .Where(d => !d.IsDaySealed && d.TotalWorkCount > 0)
            .Select(d => d.Date.ToString("yyyy-MM-dd"))
            .ToList();

        var scopeLabel = groupId.HasValue ? $" for group '{groupName}'" : " across all groups";
        var openNote = openDayList.Count > 0
            ? $" Unsealed days with works: {string.Join(", ", openDayList.Take(15))}" +
              (openDayList.Count > 15 ? $" … and {openDayList.Count - 15} more." : ".")
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                StartDate = startDate,
                EndDate = endDate,
                GroupId = groupId,
                GroupName = groupName,
                TotalDays = days.Count,
                SealedDays = sealedDays,
                PartiallySealedDays = partialDays,
                OpenDaysWithWorks = openDays,
                EmptyDays = emptyDays,
                IsFullySealed = fullySealed
            },
            $"Period {startDate}..{endDate}{scopeLabel}: {sealedDays} of {days.Count} day(s) sealed, " +
            $"{partialDays} partially sealed, {openDays} open with works, {emptyDays} empty. " +
            (fullySealed ? "The period is FULLY sealed." : "The period is NOT fully sealed.") + openNote);
    }
}
