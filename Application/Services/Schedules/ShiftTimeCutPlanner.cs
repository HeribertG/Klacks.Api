// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Plans cut_shift: parses the parts, resolves the plannable shift to cut (an order id resolves to its plannable shift;
/// an order that is already split or has no plannable shift is refused), checks that the parts tile the order span and
/// builds the cut batch. SPLIT RULE: the plannable shift itself is REDUCED to the first part (UPDATE, OriginalShift ->
/// SplitShift), the remaining parts are created, so no duplicate plannable shift is left over; the order (SealedOrder) is
/// never changed. The plan carries the default expenses of the plannable shift, which every part carries after the cut
/// (the same rule ShiftCutFacade copies by). Writes nothing; the skill and its confirmation preview both plan through here.
/// </summary>
/// <param name="shiftRepository">Loads the shift and its groups</param>
/// <param name="shiftReader">Finds the plannable shift and existing parts of an order</param>
/// <param name="shiftVisibilityGuard">Answers a hidden shift like a missing one</param>
/// <param name="shiftExpensesRepository">Reads the default expenses of the plannable shift</param>

using System.Globalization;
using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Common;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Services.Schedules;

public class ShiftTimeCutPlanner : IShiftTimeCutPlanner
{
    private const int MinParts = 2;
    private const int MaxParts = 12;
    private const int SingleWordAbbreviationLength = 3;
    private const int MaxInitials = 3;
    private const int MaxAbbreviationLength = 10;
    private const char PartSeparator = ',';
    private const char RangeSeparator = '-';
    private const char WordSeparator = ' ';

    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftReader _shiftReader;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IShiftExpensesRepository _shiftExpensesRepository;

    public ShiftTimeCutPlanner(
        IShiftRepository shiftRepository,
        IShiftReader shiftReader,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IShiftExpensesRepository shiftExpensesRepository)
    {
        _shiftExpensesRepository = shiftExpensesRepository;
        _shiftRepository = shiftRepository;
        _shiftReader = shiftReader;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<ShiftTimeCutPlan> PlanAsync(
        Guid shiftId,
        string partsRaw,
        string partNamesRaw,
        string? userLanguage,
        CancellationToken cancellationToken = default)
    {
        var (ranges, rangeError) = ParseParts(partsRaw, userLanguage);
        if (ranges is null)
        {
            return ShiftTimeCutPlan.Refuse(rangeError!);
        }

        var names = partNamesRaw
            .Split(PartSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var (originalShift, sealedOrderId, targetError) = await ResolveCutTargetAsync(shiftId, cancellationToken);
        if (originalShift is null)
        {
            return ShiftTimeCutPlan.Refuse(targetError!);
        }

        var tilingError = ShiftTilingValidator.Validate(originalShift.StartShift, originalShift.EndShift, ranges);
        if (tilingError != null)
        {
            return ShiftTimeCutPlan.Refuse(
                $"{tilingError} Adjust the parts so they cover the order span exactly, then call cut_shift again.");
        }

        var groups = await _shiftRepository.GetGroupsForShift(originalShift.Id);
        var groupResources = groups.Select(g => new SimpleGroupResource { Id = g.Id }).ToList();

        var parts = BuildParts(originalShift, ranges, names);
        var operations = parts
            .Select(part => new CutOperation
            {
                Type = part.Converted ? CutOperation.UpdateType : CutOperation.CreateType,
                ParentId = sealedOrderId.ToString(),
                Data = BuildPartResource(originalShift, sealedOrderId, part, groupResources)
            })
            .ToList();

        var defaultExpenses = await _shiftExpensesRepository.GetByShiftIdsAsync([originalShift.Id], cancellationToken);

        return new ShiftTimeCutPlan(null, originalShift, sealedOrderId, operations, parts, groupResources, defaultExpenses);
    }

    private static (List<(TimeOnly Start, TimeOnly End)>? Ranges, string? Error) ParseParts(string partsRaw, string? language)
    {
        var ranges = ParseRanges(partsRaw, language, out var rangeError);
        if (rangeError != null)
        {
            return (null, rangeError);
        }

        if (ranges.Count < MinParts || ranges.Count > MaxParts)
        {
            return (null, $"A cut needs between {MinParts} and {MaxParts} parts; received {ranges.Count}.");
        }

        return (ranges, null);
    }

    private async Task<(Shift? OriginalShift, Guid SealedOrderId, string? Error)> ResolveCutTargetAsync(
        Guid shiftId, CancellationToken cancellationToken)
    {
        var loadedShift = await _shiftRepository.Get(shiftId);
        if (loadedShift == null || !await _shiftVisibilityGuard.IsVisibleAsync(loadedShift, cancellationToken))
        {
            return (null, Guid.Empty, $"Shift with ID {shiftId} not found.");
        }

        var sealedOrderId = ResolveSealedOrderId(loadedShift);
        if (sealedOrderId == null)
        {
            return (null, Guid.Empty,
                $"Shift '{loadedShift.Name}' has status {loadedShift.Status} and has no order reference and cannot be cut. " +
                "Create the order first (create_shift), then cut its plannable shift.");
        }

        var alreadySplit = await _shiftReader.AnyByOriginalIdAsync(
            sealedOrderId.Value, ShiftStatus.SplitShift, cancellationToken);
        if (alreadySplit)
        {
            return (null, Guid.Empty,
                $"Order '{loadedShift.Name}' is already split into parts. Reuse the existing parts or reset the cut first " +
                "instead of cutting again (use find_split_shift_candidates to inspect the existing parts).");
        }

        var originalShift = loadedShift.Status == ShiftStatus.OriginalShift
            ? loadedShift
            : await _shiftReader.FindFirstByOriginalIdAsync(
                sealedOrderId.Value, ShiftStatus.OriginalShift, cancellationToken);

        if (originalShift == null)
        {
            return (null, Guid.Empty,
                $"Order '{loadedShift.Name}' has no plannable shift to cut. Create the order first (create_shift).");
        }

        return (originalShift, sealedOrderId.Value, null);
    }

    private static List<ShiftTimeCutPart> BuildParts(
        Shift originalShift, List<(TimeOnly Start, TimeOnly End)> ranges, List<string> names)
    {
        var parts = new List<ShiftTimeCutPart>();
        for (var i = 0; i < ranges.Count; i++)
        {
            var (start, end) = ranges[i];
            var name = i < names.Count ? names[i] : $"{originalShift.Name} ({FormatTime(start)}-{FormatTime(end)})";
            var isConversion = i == 0;
            parts.Add(new ShiftTimeCutPart(
                isConversion ? originalShift.Id : Guid.NewGuid(),
                i,
                name,
                start,
                end,
                WorkHours(start, end),
                end <= start,
                isConversion));
        }

        return parts;
    }

    private static ShiftResource BuildPartResource(
        Shift originalShift, Guid sealedOrderId, ShiftTimeCutPart part, List<SimpleGroupResource> groupResources)
    {
        return new ShiftResource
        {
            Id = part.Id,
            Name = part.Name,
            Abbreviation = BuildAbbreviation(part.Name, part.Index),
            Description = originalShift.Description,
            Status = ShiftStatus.SplitShift,
            ShiftType = originalShift.ShiftType,
            OriginalId = sealedOrderId,
            ClientId = originalShift.ClientId,
            MacroId = originalShift.MacroId,
            StartShift = part.Start,
            EndShift = part.End,
            FromDate = originalShift.FromDate,
            UntilDate = originalShift.UntilDate,
            WorkTime = part.WorkTime,
            CuttingAfterMidnight = part.CuttingAfterMidnight,
            SumEmployees = originalShift.SumEmployees,
            Quantity = originalShift.Quantity,
            IsMonday = originalShift.IsMonday,
            IsTuesday = originalShift.IsTuesday,
            IsWednesday = originalShift.IsWednesday,
            IsThursday = originalShift.IsThursday,
            IsFriday = originalShift.IsFriday,
            IsSaturday = originalShift.IsSaturday,
            IsSunday = originalShift.IsSunday,
            IsHoliday = originalShift.IsHoliday,
            IsWeekdayAndHoliday = originalShift.IsWeekdayAndHoliday,
            IsTimeRange = originalShift.IsTimeRange,
            IsSporadic = originalShift.IsSporadic,
            SporadicScope = originalShift.SporadicScope,
            Groups = groupResources
        };
    }

    private static Guid? ResolveSealedOrderId(Shift shift)
    {
        return shift.Status switch
        {
            ShiftStatus.SealedOrder => shift.Id,
            ShiftStatus.OriginalShift => shift.OriginalId,
            ShiftStatus.SplitShift => shift.OriginalId,
            _ => null
        };
    }

    private static List<(TimeOnly Start, TimeOnly End)> ParseRanges(string raw, string? language, out string? error)
    {
        error = null;
        var ranges = new List<(TimeOnly, TimeOnly)>();
        var entries = raw.Split(PartSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var entry in entries)
        {
            var dash = entry.IndexOf(RangeSeparator);
            if (dash <= 0 || dash >= entry.Length - 1)
            {
                error = $"Invalid part '{entry}'. Expected format 'HH:mm-HH:mm'.";
                return ranges;
            }

            var startStr = entry[..dash].Trim();
            var endStr = entry[(dash + 1)..].Trim();

            if (!SkillCalendarStringParser.TryParseTimeOnly(startStr, language, out var start)
                || !SkillCalendarStringParser.TryParseTimeOnly(endStr, language, out var end))
            {
                error = $"Invalid time in part '{entry}'. Expected format 'HH:mm-HH:mm'.";
                return ranges;
            }

            ranges.Add((start, end));
        }

        return ranges;
    }

    private static string BuildAbbreviation(string name, int index)
    {
        var words = name.Split(WordSeparator, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return $"P{index + 1}";
        }

        var abbreviation = words.Length == 1
            ? (words[0].Length >= SingleWordAbbreviationLength
                ? words[0][..SingleWordAbbreviationLength].ToUpperInvariant()
                : words[0].ToUpperInvariant())
            : string.Concat(words.Take(MaxInitials).Select(word => char.ToUpperInvariant(word[0])));

        return abbreviation.Length > MaxAbbreviationLength ? abbreviation[..MaxAbbreviationLength] : abbreviation;
    }

    private static decimal WorkHours(TimeOnly start, TimeOnly end)
    {
        var duration = end.ToTimeSpan() - start.ToTimeSpan();
        if (duration.TotalHours <= 0)
        {
            duration = duration.Add(TimeSpan.FromHours(DayTimeConstants.HoursPerDay));
        }

        return (decimal)duration.TotalHours;
    }

    private static string FormatTime(TimeOnly time) => time.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture);
}
