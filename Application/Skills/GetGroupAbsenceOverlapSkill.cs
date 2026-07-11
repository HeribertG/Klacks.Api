// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only advisory skill that shows the team absence overlap picture of a group's active members
/// over a date range: per day it reports how many and which members are absent, with booked
/// (Break) and planned (BreakPlaceholder) absences counted separately, plus the peak day(s) of
/// simultaneous absence and their share of the group's size. Lets the assistant inform the user
/// before planning further absences (e.g. "In week 30, 4 of 8 are already away").
/// </summary>
/// <param name="groupName">Name (or partial name) of the group to analyse.</param>
/// <param name="fromDate">First day of the analysed period (yyyy-MM-dd).</param>
/// <param name="untilDate">Last day of the analysed period (yyyy-MM-dd).</param>

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_group_absence_overlap")]
public class GetGroupAbsenceOverlapSkill : BaseSkillImplementation
{
    private const int MaxRangeDays = 366;
    private const string UnknownAbsenceLabel = "Unknown";
    private const string DateFormat = "yyyy-MM-dd";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IClientBreakPlaceholderRepository _clientBreakPlaceholderRepository;
    private readonly IAbsenceRepository _absenceRepository;

    public GetGroupAbsenceOverlapSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IClientBreakPlaceholderRepository clientBreakPlaceholderRepository,
        IAbsenceRepository absenceRepository)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _clientBreakPlaceholderRepository = clientBreakPlaceholderRepository;
        _absenceRepository = absenceRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetRequiredString(parameters, "groupName");
        var fromDate = GetParameter<DateOnly?>(parameters, "fromDate")
            ?? throw new ArgumentException("Required parameter 'fromDate' is missing");
        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate")
            ?? throw new ArgumentException("Required parameter 'untilDate' is missing");

        if (untilDate < fromDate)
        {
            return SkillResult.Error(
                $"untilDate ({untilDate.ToString(DateFormat)}) must not be before fromDate ({fromDate.ToString(DateFormat)}).");
        }

        var rangeDays = untilDate.DayNumber - fromDate.DayNumber + 1;
        if (rangeDays > MaxRangeDays)
        {
            return SkillResult.Error(
                $"The period {fromDate.ToString(DateFormat)}..{untilDate.ToString(DateFormat)} spans {rangeDays} days, " +
                $"which exceeds the maximum of {MaxRangeDays} days. Narrow the date range and try again.");
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var groups = scope.Filter(await _groupRepository.List());
        var (group, groupError) = GroupResolver.Resolve(groups, groupName);
        if (group == null)
        {
            return SkillResult.Error(groupError!);
        }

        var filter = new BreakFilter
        {
            SelectedGroup = group.Id,
            StartDate = fromDate,
            EndDate = untilDate,
            ShowEmployees = true,
            ShowExtern = true,
            OrderBy = "name",
            SortOrder = "asc"
        };

        var (clients, _) = await _clientBreakPlaceholderRepository.BreakList(filter, cancellationToken);
        var groupSize = clients.Count;

        if (groupSize == 0)
        {
            return SkillResult.SuccessResult(
                new GetGroupAbsenceOverlapResult(
                    group.Id, group.Name, fromDate, untilDate, 0, 0,
                    Array.Empty<DateOnly>(), Array.Empty<GroupAbsenceOverlapDay>()),
                $"Group '{group.Name}' has no active members in " +
                $"{fromDate.ToString(DateFormat)}..{untilDate.ToString(DateFormat)}.");
        }

        var absenceLabels = (await _absenceRepository.List())
            .ToDictionary(a => a.Id, a => Label(a.Name));

        var bookedByDay = new Dictionary<DateOnly, List<GroupAbsenceOverlapMember>>();
        var plannedByDay = new Dictionary<DateOnly, List<GroupAbsenceOverlapMember>>();

        foreach (var client in clients)
        {
            var clientName = $"{client.FirstName} {client.Name}".Trim();

            foreach (var brk in client.Breaks.Where(b => !b.IsDeleted))
            {
                var member = new GroupAbsenceOverlapMember(
                    client.Id, clientName, absenceLabels.GetValueOrDefault(brk.AbsenceId, UnknownAbsenceLabel));
                AddToDay(bookedByDay, brk.CurrentDate, member);
            }

            foreach (var placeholder in client.BreakPlaceholders.Where(bp => !bp.IsDeleted))
            {
                var clippedFrom = Max(DateOnly.FromDateTime(placeholder.From), fromDate);
                var clippedUntil = Min(DateOnly.FromDateTime(placeholder.Until), untilDate);
                var member = new GroupAbsenceOverlapMember(
                    client.Id, clientName, absenceLabels.GetValueOrDefault(placeholder.AbsenceId, UnknownAbsenceLabel));

                for (var day = clippedFrom; day <= clippedUntil; day = day.AddDays(1))
                {
                    AddToDay(plannedByDay, day, member);
                }
            }
        }

        var days = bookedByDay.Keys
            .Concat(plannedByDay.Keys)
            .Distinct()
            .OrderBy(d => d)
            .Select(day => BuildDay(day, bookedByDay, plannedByDay, groupSize))
            .ToList();

        var peakCount = days.Count == 0 ? 0 : days.Max(d => d.TotalAbsentCount);
        var peakDates = days
            .Where(d => peakCount > 0 && d.TotalAbsentCount == peakCount)
            .Select(d => d.Date)
            .ToList();

        var result = new GetGroupAbsenceOverlapResult(
            group.Id, group.Name, fromDate, untilDate, groupSize, peakCount, peakDates, days);

        var message = BuildMessage(group.Name, groupSize, fromDate, untilDate, peakCount, peakDates);

        return SkillResult.SuccessResult(result, message);
    }

    private static GroupAbsenceOverlapDay BuildDay(
        DateOnly day,
        Dictionary<DateOnly, List<GroupAbsenceOverlapMember>> bookedByDay,
        Dictionary<DateOnly, List<GroupAbsenceOverlapMember>> plannedByDay,
        int groupSize)
    {
        var booked = DistinctByClient(bookedByDay.GetValueOrDefault(day));
        var planned = DistinctByClient(plannedByDay.GetValueOrDefault(day));
        var totalAbsent = booked.Select(m => m.ClientId)
            .Concat(planned.Select(m => m.ClientId))
            .Distinct()
            .Count();
        var percentAbsent = Math.Round(totalAbsent * 100.0 / groupSize, 1);

        return new GroupAbsenceOverlapDay(day, booked.Count, booked, planned.Count, planned, totalAbsent, percentAbsent);
    }

    private static string BuildMessage(
        string groupName,
        int groupSize,
        DateOnly fromDate,
        DateOnly untilDate,
        int peakCount,
        IReadOnlyList<DateOnly> peakDates)
    {
        if (peakCount == 0)
        {
            return $"Group '{groupName}' has {groupSize} active member(s) in " +
                   $"{fromDate.ToString(DateFormat)}..{untilDate.ToString(DateFormat)} and no booked or planned absences in that period.";
        }

        var peakDatesText = string.Join(", ", peakDates.Select(d => d.ToString(DateFormat)));
        var peakPercent = Math.Round(peakCount * 100.0 / groupSize, 1);

        return $"Group '{groupName}' has {groupSize} active member(s) in " +
               $"{fromDate.ToString(DateFormat)}..{untilDate.ToString(DateFormat)}. " +
               $"Peak overlap: {peakCount} of {groupSize} ({peakPercent}%) absent on {peakDatesText}. " +
               "Relay these numbers to the user, and if the overlap is high, ask for confirmation before " +
               "planning further absences instead of proceeding blindly.";
    }

    private static List<GroupAbsenceOverlapMember> DistinctByClient(List<GroupAbsenceOverlapMember>? members) =>
        members is null
            ? new List<GroupAbsenceOverlapMember>()
            : members.DistinctBy(m => m.ClientId).ToList();

    private static void AddToDay(
        Dictionary<DateOnly, List<GroupAbsenceOverlapMember>> byDay, DateOnly day, GroupAbsenceOverlapMember member)
    {
        if (!byDay.TryGetValue(day, out var list))
        {
            list = new List<GroupAbsenceOverlapMember>();
            byDay[day] = list;
        }

        list.Add(member);
    }

    private static DateOnly Max(DateOnly a, DateOnly b) => a > b ? a : b;

    private static DateOnly Min(DateOnly a, DateOnly b) => a < b ? a : b;

    private static string Label(MultiLanguage name) =>
        new[] { name.De, name.En, name.Fr, name.It }
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? UnknownAbsenceLabel;
}
