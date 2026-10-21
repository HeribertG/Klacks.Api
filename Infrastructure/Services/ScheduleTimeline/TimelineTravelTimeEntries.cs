// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Travel-time part of the schedule timeline checks: flags consecutive shifts of one client whose gap is shorter
/// than the travel time between their locations (Error) or shorter than one and a half travel times (Warning), and
/// reports once per client when no routing key is configured. Also resolves the location of a shift.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Infrastructure.Services.ScheduleTimeline;

public static class TimelineTravelTimeEntries
{
    private const double TravelTimeWarningFactor = 1.5;
    private const string GapMinutesParam = "gapMinutes";
    private const string TravelMinutesParam = "travelMinutes";

    /// <summary>
    /// Flags consecutive shifts of one client whose gap is shorter than the travel time between their
    /// locations. Only pairs starting on the same day are compared: an overnight transition is already
    /// covered by the rest-period check, and pairing across days would invent gaps that are not one shift
    /// following another.
    /// </summary>
    /// <param name="lookupBudget">Maximum routing lookups this call may spend</param>
    /// <returns>Number of routing lookups actually spent</returns>
    public static async Task<int> AddAsync(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        Dictionary<Guid, Address?> shiftAddressLookup,
        ITravelTimeCalculationService travelTimeService,
        bool apiKeyConfigured,
        CancellationToken cancellationToken,
        int lookupBudget = int.MaxValue)
    {
        var workBlocks = timeline.Blocks
            .Where(b => b.BlockType == ScheduleBlockType.Work && b.ShiftId.HasValue)
            .OrderBy(b => b.Start)
            .ToList();

        if (workBlocks.Count < 2) return 0;

        var noApiKeyInfoAdded = false;
        var lookupsSpent = 0;

        for (var i = 0; i < workBlocks.Count - 1; i++)
        {
            var current = workBlocks[i];
            var next = workBlocks[i + 1];

            if (!TryGetLocationPair(current, next, shiftAddressLookup, out var fromAddress, out var toAddress)) continue;

            if (!apiKeyConfigured)
            {
                if (!noApiKeyInfoAdded)
                {
                    entries.Add(Entry(ScheduleValidationType.Info, ScheduleValidationKeys.TravelTimeNoApiKey, timeline, clientName, current.OwnerDate));
                    noApiKeyInfoAdded = true;
                }
                continue;
            }

            if (lookupsSpent >= lookupBudget) break;

            var gap = current.GapTo(next);
            var travelTime = await travelTimeService.CalculateTravelTimeAsync(fromAddress, toAddress, cancellationToken);
            lookupsSpent++;

            if (!travelTime.HasValue) continue;

            AddGapEntry(entries, timeline, clientName, current.OwnerDate, gap, travelTime.Value);
        }

        return lookupsSpent;
    }

    public static Dictionary<Guid, Address?> BuildShiftAddressLookup(List<Work> works)
    {
        var lookup = new Dictionary<Guid, Address?>();
        foreach (var work in works)
        {
            if (lookup.ContainsKey(work.ShiftId)) continue;
            lookup[work.ShiftId] = LatestAddress(work.Shift?.Client);
        }
        return lookup;
    }

    public static Address? LatestAddress(Client? client)
        => client?.Addresses
            .OrderByDescending(a => a.ValidFrom)
            .FirstOrDefault();

    private static bool TryGetLocationPair(
        ScheduleBlock current,
        ScheduleBlock next,
        Dictionary<Guid, Address?> shiftAddressLookup,
        out Address fromAddress,
        out Address toAddress)
    {
        fromAddress = null!;
        toAddress = null!;

        if (current.OwnerDate != next.OwnerDate) return false;

        shiftAddressLookup.TryGetValue(current.ShiftId!.Value, out var from);
        shiftAddressLookup.TryGetValue(next.ShiftId!.Value, out var to);

        if (from == null || to == null) return false;
        if (IsSameAddress(from, to)) return false;

        fromAddress = from;
        toAddress = to;
        return true;
    }

    private static void AddGapEntry(
        List<ScheduleValidationNotificationDto> entries,
        ClientTimeline timeline,
        string clientName,
        DateOnly date,
        TimeSpan gap,
        TimeSpan travelTime)
    {
        if (gap < travelTime)
        {
            entries.Add(GapEntry(ScheduleValidationType.Error, ScheduleValidationKeys.TravelTimeError, timeline, clientName, date, gap, travelTime));
        }
        else if (gap < TimeSpan.FromTicks((long)(travelTime.Ticks * TravelTimeWarningFactor)))
        {
            entries.Add(GapEntry(ScheduleValidationType.Warning, ScheduleValidationKeys.TravelTimeWarning, timeline, clientName, date, gap, travelTime));
        }
    }

    private static ScheduleValidationNotificationDto GapEntry(
        ScheduleValidationType type,
        string comment,
        ClientTimeline timeline,
        string clientName,
        DateOnly date,
        TimeSpan gap,
        TimeSpan travelTime)
        => new()
        {
            Type = type,
            ClientId = timeline.ClientId,
            ClientName = clientName,
            Date = date,
            Comment = comment,
            CommentParams = new Dictionary<string, string>
            {
                [GapMinutesParam] = $"{gap.TotalMinutes:F0}",
                [TravelMinutesParam] = $"{travelTime.TotalMinutes:F0}"
            }
        };

    private static ScheduleValidationNotificationDto Entry(
        ScheduleValidationType type, string comment, ClientTimeline timeline, string clientName, DateOnly date)
        => new()
        {
            Type = type,
            ClientId = timeline.ClientId,
            ClientName = clientName,
            Date = date,
            Comment = comment
        };

    private static bool IsSameAddress(Address a, Address b)
    {
        if (a.Id == b.Id) return true;
        return string.Equals(a.Street, b.Street, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.Zip, b.Zip, StringComparison.OrdinalIgnoreCase)
            && string.Equals(a.City, b.City, StringComparison.OrdinalIgnoreCase);
    }
}
