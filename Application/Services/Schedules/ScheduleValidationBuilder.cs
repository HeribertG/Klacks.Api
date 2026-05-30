// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure validation builders shared between the SignalR background validator and the
/// on-demand period-issues loader. Given a ClientTimeline, a name and a policy, each
/// helper appends rest-violation / overtime / consecutive-day entries to the supplied list.
/// </summary>
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Services.Schedules;

public static class ScheduleValidationBuilder
{
    private const int DaysPerWeek = 7;
    public static void AddRestViolations(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        SchedulingPolicy policy)
    {
        foreach (var violation in timeline.GetRestViolations(policy.MinRestHours))
        {
            entries.Add(new ScheduleValidationNotificationDto
            {
                Type = ScheduleValidationType.Warning,
                ClientId = timeline.ClientId,
                ClientName = clientName,
                Date = violation.PreviousBlock.OwnerDate,
                Comment = "schedule.error-list.rest-violation",
                CommentParams = new Dictionary<string, string>
                {
                    ["actualHours"] = $"{violation.ActualRest.TotalHours:F1}",
                    ["requiredHours"] = $"{violation.RequiredRest.TotalHours:F0}",
                    ["endTime"] = $"{violation.PreviousBlock.End:HH:mm}",
                    ["startTime"] = $"{violation.NextBlock.Start:HH:mm}"
                }
            });
        }
    }

    public static void AddOvertime(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        DateOnly startDate,
        DateOnly endDate,
        SchedulingPolicy policy)
    {
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var duration = timeline.GetWorkDuration(date);
            if (duration > policy.MaxDailyHours)
            {
                entries.Add(new ScheduleValidationNotificationDto
                {
                    Type = ScheduleValidationType.Warning,
                    ClientId = timeline.ClientId,
                    ClientName = clientName,
                    Date = date,
                    Comment = "schedule.error-list.overtime",
                    CommentParams = new Dictionary<string, string>
                    {
                        ["actualHours"] = $"{duration.TotalHours:F1}",
                        ["maxHours"] = $"{policy.MaxDailyHours.TotalHours:F0}"
                    }
                });
            }
        }
    }

    public static void AddConsecutiveDays(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        DateOnly startDate,
        DateOnly endDate,
        SchedulingPolicy policy)
    {
        var date = startDate;
        while (date <= endDate)
        {
            var consecutive = timeline.GetConsecutiveWorkDays(date);
            if (consecutive > policy.MaxConsecutiveDays)
            {
                entries.Add(new ScheduleValidationNotificationDto
                {
                    Type = ScheduleValidationType.Warning,
                    ClientId = timeline.ClientId,
                    ClientName = clientName,
                    Date = date,
                    Comment = "schedule.error-list.consecutive-days",
                    CommentParams = new Dictionary<string, string>
                    {
                        ["actualDays"] = consecutive.ToString(),
                        ["maxDays"] = policy.MaxConsecutiveDays.ToString()
                    }
                });
                date = date.AddDays(consecutive);
            }
            else
            {
                date = date.AddDays(1);
            }
        }
    }

    public static void AddWeeklyOvertime(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        DateOnly startDate,
        DateOnly endDate,
        SchedulingPolicy policy)
    {
        // Partial weeks at the period boundary can only under-count hours (days outside the loaded
        // period contribute zero), so every week flagged here is a real violation — no false positives.
        for (var weekStart = MondayOf(startDate); weekStart <= endDate; weekStart = weekStart.AddDays(DaysPerWeek))
        {
            var duration = timeline.GetWeeklyWorkDuration(weekStart);
            if (duration <= policy.MaxWeeklyHours) continue;

            var reportDate = weekStart < startDate ? startDate : weekStart;
            entries.Add(new ScheduleValidationNotificationDto
            {
                Type = ScheduleValidationType.Warning,
                ClientId = timeline.ClientId,
                ClientName = clientName,
                Date = reportDate,
                Comment = "schedule.error-list.weekly-overtime",
                CommentParams = new Dictionary<string, string>
                {
                    ["actualHours"] = $"{duration.TotalHours:F1}",
                    ["maxHours"] = $"{policy.MaxWeeklyHours.TotalHours:F0}"
                }
            });
        }
    }

    public static void AddMinRestDays(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        DateOnly startDate,
        DateOnly endDate,
        SchedulingPolicy policy)
    {
        // Only evaluate ISO weeks lying completely within [startDate, endDate]. A partial boundary
        // week has days outside the loaded period that would be miscounted as rest, so judging it
        // would be unreliable (the MinRestDays-spillover trap). Full weeks only.
        for (var weekStart = MondayOf(startDate); weekStart.AddDays(DaysPerWeek - 1) <= endDate; weekStart = weekStart.AddDays(DaysPerWeek))
        {
            if (weekStart < startDate) continue;

            var restDays = timeline.GetRestDayCount(weekStart);
            if (restDays >= policy.MinRestDays) continue;

            entries.Add(new ScheduleValidationNotificationDto
            {
                Type = ScheduleValidationType.Warning,
                ClientId = timeline.ClientId,
                ClientName = clientName,
                Date = weekStart,
                Comment = "schedule.error-list.min-rest-days",
                CommentParams = new Dictionary<string, string>
                {
                    ["actualDays"] = restDays.ToString(),
                    ["minDays"] = policy.MinRestDays.ToString()
                }
            });
        }
    }

    public static void AddCollisions(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName)
    {
        foreach (var pair in timeline.GetCollisions())
        {
            entries.Add(new ScheduleValidationNotificationDto
            {
                Type = ScheduleValidationType.Error,
                ClientId = timeline.ClientId,
                ClientName = clientName,
                Date = pair.A.OwnerDate,
                Comment = "schedule.error-list.collision",
                CommentParams = new Dictionary<string, string>
                {
                    ["type1"] = pair.A.BlockType.ToString(),
                    ["timeRange1"] = $"{pair.A.Start:HH:mm} - {pair.A.End:HH:mm}",
                    ["type2"] = pair.B.BlockType.ToString(),
                    ["timeRange2"] = $"{pair.B.Start:HH:mm} - {pair.B.End:HH:mm}"
                }
            });
        }
    }

    private static DateOnly MondayOf(DateOnly date)
    {
        var offsetFromMonday = ((int)date.DayOfWeek + DaysPerWeek - 1) % DaysPerWeek;
        return date.AddDays(-offsetFromMonday);
    }
}
