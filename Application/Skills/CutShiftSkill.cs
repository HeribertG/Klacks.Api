// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that cuts ONE existing plannable shift (OriginalShift, e.g. a 24h order 07:00-07:00)
/// into several time parts (split shifts).
/// SPLIT RULE: the plannable shift itself is REDUCED to the first part — its end becomes the start
/// of the next part — and its status changes OriginalShift(2) -> SplitShift(3). The remaining parts
/// are created. So no duplicate plannable shift is left over next to its parts.
/// The order (Bestellung / SealedOrder, status 1) is IMMUTABLE and is NEVER changed by a cut.
/// This is the correct way to model an early/late/night rotation: create ONE order, then cut it here.
/// </summary>
/// <param name="shiftId">UUID of the plannable shift (OriginalShift) or its order to cut.</param>
/// <param name="parts">Comma-separated time ranges "HH:mm-HH:mm" that tile the order span (e.g. "07:00-15:00,15:00-23:00,23:00-07:00").</param>
/// <param name="partNames">Optional comma-separated names for the parts, aligned by index (e.g. "Frühdienst,Spätdienst,Nachtdienst").</param>

using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("cut_shift")]
public class CutShiftSkill : BaseSkillImplementation
{
    private const int MinParts = 2;
    private const int MaxParts = 12;

    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftCutFacade _shiftCutFacade;

    public CutShiftSkill(IShiftRepository shiftRepository, IShiftCutFacade shiftCutFacade)
    {
        _shiftRepository = shiftRepository;
        _shiftCutFacade = shiftCutFacade;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");
        var partsRaw = GetRequiredString(parameters, "parts");
        var namesRaw = GetParameter<string>(parameters, "partNames") ?? string.Empty;

        var ranges = ParseRanges(partsRaw, out var rangeError);
        if (rangeError != null)
        {
            return SkillResult.Error(rangeError);
        }

        if (ranges.Count < MinParts || ranges.Count > MaxParts)
        {
            return SkillResult.Error($"A cut needs between {MinParts} and {MaxParts} parts; received {ranges.Count}.");
        }

        var names = namesRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var loadedShift = await _shiftRepository.Get(shiftId);
        if (loadedShift == null)
        {
            return SkillResult.Error($"Shift with ID {shiftId} not found.");
        }

        var sealedOrderId = ResolveSealedOrderId(loadedShift);
        if (sealedOrderId == null)
        {
            return SkillResult.Error(
                $"Shift '{loadedShift.Name}' has status {loadedShift.Status} and has no order reference and cannot be cut. " +
                "Create the order first (create_shift), then cut its plannable shift.");
        }

        var alreadySplit = await _shiftRepository.GetQuery()
            .AnyAsync(s => s.OriginalId == sealedOrderId && s.Status == ShiftStatus.SplitShift, cancellationToken);
        if (alreadySplit)
        {
            return SkillResult.Error(
                $"Order '{loadedShift.Name}' is already split into parts. Reuse the existing parts or reset the cut first " +
                "instead of cutting again (use find_split_shift_candidates to inspect the existing parts).");
        }

        var originalShift = loadedShift.Status == ShiftStatus.OriginalShift
            ? loadedShift
            : await _shiftRepository.GetQuery()
                .FirstOrDefaultAsync(s => s.OriginalId == sealedOrderId && s.Status == ShiftStatus.OriginalShift, cancellationToken);

        if (originalShift == null)
        {
            return SkillResult.Error(
                $"Order '{loadedShift.Name}' has no plannable shift to cut. Create the order first (create_shift).");
        }

        var groups = await _shiftRepository.GetGroupsForShift(originalShift.Id);
        var groupResources = groups.Select(g => new SimpleGroupResource { Id = g.Id }).ToList();

        var operations = new List<CutOperation>();
        var summaries = new List<object>();

        for (var i = 0; i < ranges.Count; i++)
        {
            var (start, end) = ranges[i];
            var partName = i < names.Count ? names[i] : $"{originalShift.Name} ({start:HH\\:mm}-{end:HH\\:mm})";
            var crossesMidnight = end <= start;

            // The first part REUSES the plannable shift (UPDATE: OriginalShift -> SplitShift), so the
            // order is converted instead of left over as a duplicate. Further parts are created.
            var isConversion = i == 0;
            var partId = isConversion ? originalShift.Id : Guid.NewGuid();

            var data = new ShiftResource
            {
                Id = partId,
                Name = partName,
                Abbreviation = BuildAbbreviation(partName, i),
                Description = originalShift.Description,
                Status = ShiftStatus.SplitShift,
                ShiftType = originalShift.ShiftType,
                OriginalId = sealedOrderId,
                ClientId = originalShift.ClientId,
                MacroId = originalShift.MacroId,
                StartShift = start,
                EndShift = end,
                FromDate = originalShift.FromDate,
                UntilDate = originalShift.UntilDate,
                WorkTime = CalculateWorkTime(start, end),
                CuttingAfterMidnight = crossesMidnight,
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

            operations.Add(new CutOperation
            {
                Type = isConversion ? "UPDATE" : "CREATE",
                ParentId = sealedOrderId.Value.ToString(),
                Data = data
            });

            summaries.Add(new
            {
                data.Id,
                data.Name,
                StartTime = start.ToString("HH\\:mm"),
                EndTime = end.ToString("HH\\:mm"),
                WorkTime = data.WorkTime,
                data.CuttingAfterMidnight,
                Converted = isConversion
            });
        }

        var results = await _shiftCutFacade.ProcessBatchCutsAsync(operations);

        var resultData = new
        {
            SealedOrderId = sealedOrderId,
            OrderName = originalShift.Name,
            PartCount = results.Count,
            GroupCount = groupResources.Count,
            Parts = summaries
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Cut the plannable shift '{originalShift.Name}' into {results.Count} part(s): " +
            string.Join(", ", summaries.Select(s => ((dynamic)s).Name)) +
            ". The first part reuses the plannable shift (reduced, status -> SplitShift); " +
            "the order (Bestellung) itself stays unchanged.");
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

    private static List<(TimeOnly Start, TimeOnly End)> ParseRanges(string raw, out string? error)
    {
        error = null;
        var ranges = new List<(TimeOnly, TimeOnly)>();
        var entries = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var entry in entries)
        {
            var dash = entry.IndexOf('-');
            if (dash <= 0 || dash >= entry.Length - 1)
            {
                error = $"Invalid part '{entry}'. Expected format 'HH:mm-HH:mm'.";
                return ranges;
            }

            var startStr = entry[..dash].Trim();
            var endStr = entry[(dash + 1)..].Trim();

            if (!TimeOnly.TryParse(startStr, out var start) || !TimeOnly.TryParse(endStr, out var end))
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
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return $"P{index + 1}";
        }

        var abbreviation = parts.Length == 1
            ? (parts[0].Length >= 3 ? parts[0][..3].ToUpperInvariant() : parts[0].ToUpperInvariant())
            : string.Concat(parts.Take(3).Select(p => char.ToUpperInvariant(p[0])));

        return abbreviation.Length > 10 ? abbreviation[..10] : abbreviation;
    }
}
