// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that cuts ONE plannable shift piece (an OriginalShift, or an already-split SplitShift part) at
/// a calendar date boundary, mirroring the frontend's cut-by-date flow: the existing piece is shrunk so
/// its UntilDate ends the day before cutDate (status forced to SplitShift), and a new piece is created
/// starting on cutDate that inherits the old UntilDate, groups and weekly pattern. Cutting the plannable
/// shift of an uncut order produces two flat sibling parts of that order (like cut_shift); cutting an
/// already-split part nests the new piece as its child (sub-cut), matching the documented cut-tree
/// conventions (root-id/parent-id semantics of ShiftCutFacade).
/// </summary>
/// <param name="shiftId">UUID of the plannable shift (OriginalShift), an existing split part (SplitShift), or the order — an order id is resolved to its single not-yet-split plannable shift.</param>
/// <param name="cutDate">ISO date (yyyy-MM-dd) where the cut happens; the new piece starts on this date.</param>
/// <param name="newName">Optional name for the new piece; defaults to the cut piece's own name.</param>
/// <param name="newAbbreviation">Optional abbreviation for the new piece; defaults to the cut piece's own abbreviation.</param>

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

[SkillImplementation("cut_shift_by_date")]
public class CutShiftByDateSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftCutFacade _shiftCutFacade;

    public CutShiftByDateSkill(IShiftRepository shiftRepository, IShiftCutFacade shiftCutFacade)
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
        var cutDate = GetParameter<DateOnly?>(parameters, "cutDate")
            ?? throw new ArgumentException("Required parameter 'cutDate' is missing");
        var newName = GetParameter<string>(parameters, "newName");
        var newAbbreviation = GetParameter<string>(parameters, "newAbbreviation");

        var loadedShift = await _shiftRepository.Get(shiftId);
        if (loadedShift == null)
        {
            return SkillResult.Error($"Shift with ID {shiftId} not found.");
        }

        var (target, resolveError) = await ResolveTarget(loadedShift, cancellationToken);
        if (resolveError != null)
        {
            return SkillResult.Error(resolveError);
        }

        var sealedOrderId = target!.OriginalId;
        if (sealedOrderId == null)
        {
            return SkillResult.Error($"Shift '{target.Name}' has no order reference (OriginalId) and cannot be cut.");
        }

        if (cutDate <= target.FromDate || (target.UntilDate.HasValue && cutDate > target.UntilDate.Value))
        {
            var upper = target.UntilDate.HasValue ? target.UntilDate.Value.ToString() : "open-ended";
            return SkillResult.Error(
                $"cutDate ({cutDate}) must be strictly after fromDate ({target.FromDate}) and on or before untilDate ({upper}) of '{target.Name}'.");
        }

        var wasOriginalShift = target.Status == ShiftStatus.OriginalShift;

        var groups = await _shiftRepository.GetGroupsForShift(target.Id);
        var groupResources = groups.Select(g => new SimpleGroupResource { Id = g.Id }).ToList();

        var updateData = BuildPieceResource(target, groupResources);
        updateData.UntilDate = cutDate.AddDays(-1);
        updateData.Status = ShiftStatus.SplitShift;
        if (!wasOriginalShift)
        {
            updateData.ParentId = target.ParentId;
            updateData.RootId = target.RootId;
            updateData.Lft = target.Lft;
            updateData.Rgt = target.Rgt;
        }

        var newPartId = Guid.NewGuid();
        var createData = BuildPieceResource(target, groupResources);
        createData.Id = newPartId;
        createData.Name = string.IsNullOrWhiteSpace(newName) ? target.Name : newName;
        createData.Abbreviation = string.IsNullOrWhiteSpace(newAbbreviation) ? target.Abbreviation : newAbbreviation;
        createData.FromDate = cutDate;
        createData.UntilDate = target.UntilDate;
        createData.Status = ShiftStatus.SplitShift;

        var createParentId = wasOriginalShift ? sealedOrderId.Value : target.Id;

        var operations = new List<CutOperation>
        {
            new CutOperation
            {
                Type = "UPDATE",
                ParentId = sealedOrderId.Value.ToString(),
                Data = updateData
            },
            new CutOperation
            {
                Type = "CREATE",
                ParentId = createParentId.ToString(),
                Data = createData
            }
        };

        await _shiftCutFacade.ProcessBatchCutsAsync(operations);

        var freshCuts = await _shiftRepository.CutList(sealedOrderId.Value);
        var updatedPiece = freshCuts.FirstOrDefault(s => s.Id == target.Id);
        var newPiece = freshCuts.FirstOrDefault(s => s.Id == newPartId);

        var verified = updatedPiece != null
            && updatedPiece.UntilDate == cutDate.AddDays(-1)
            && newPiece != null
            && newPiece.FromDate == cutDate;

        if (!verified)
        {
            return SkillResult.Error(
                $"Database verification failed: could not confirm the cut of '{target.Name}' at {cutDate} " +
                "in the database after the write. The write may still have been applied (this skill does not roll back); " +
                "check the cut list (list_shift_cuts) before retrying to avoid a duplicate cut.");
        }

        return SkillResult.SuccessResult(
            new
            {
                SealedOrderId = sealedOrderId,
                OriginalPieceId = target.Id,
                OriginalPieceUntilDate = updatedPiece!.UntilDate,
                NewPieceId = newPartId,
                NewPieceName = createData.Name,
                NewPieceFromDate = cutDate,
                Verified = true
            },
            $"Cut '{target.Name}' at {cutDate}: the existing piece now ends {updatedPiece.UntilDate}, " +
            $"and a new piece '{createData.Name}' starts {cutDate}. Confirmed in the database (verified).");
    }

    private async Task<(Shift? Target, string? Error)> ResolveTarget(Shift loadedShift, CancellationToken cancellationToken)
    {
        switch (loadedShift.Status)
        {
            case ShiftStatus.OriginalShift:
            case ShiftStatus.SplitShift:
                return (loadedShift, null);

            case ShiftStatus.SealedOrder:
                var originalShift = await _shiftRepository.GetQuery()
                    .FirstOrDefaultAsync(s => s.OriginalId == loadedShift.Id && s.Status == ShiftStatus.OriginalShift, cancellationToken);
                if (originalShift != null)
                {
                    return (originalShift, null);
                }

                var partCount = await _shiftRepository.GetQuery()
                    .CountAsync(s => s.OriginalId == loadedShift.Id && s.Status == ShiftStatus.SplitShift, cancellationToken);
                if (partCount > 0)
                {
                    return (null, $"Order '{loadedShift.Name}' is already split into {partCount} part(s). " +
                        "Pass the shiftId of the specific part to cut further (use list_shift_cuts to find it).");
                }

                return (null, $"Order '{loadedShift.Name}' has no plannable shift to cut. Create it first (create_shift).");

            default:
                return (null, $"Shift '{loadedShift.Name}' has status {loadedShift.Status} and cannot be cut by date. " +
                    "Seal the order first, then cut its plannable shift.");
        }
    }

    private static ShiftResource BuildPieceResource(Shift source, List<SimpleGroupResource> groupResources)
    {
        return new ShiftResource
        {
            Id = source.Id,
            Name = source.Name,
            Abbreviation = source.Abbreviation,
            Description = source.Description,
            ShiftType = source.ShiftType,
            OriginalId = source.OriginalId,
            ClientId = source.ClientId,
            MacroId = source.MacroId,
            StartShift = source.StartShift,
            EndShift = source.EndShift,
            FromDate = source.FromDate,
            UntilDate = source.UntilDate,
            WorkTime = source.WorkTime,
            CuttingAfterMidnight = source.CuttingAfterMidnight,
            SumEmployees = source.SumEmployees,
            Quantity = source.Quantity,
            IsMonday = source.IsMonday,
            IsTuesday = source.IsTuesday,
            IsWednesday = source.IsWednesday,
            IsThursday = source.IsThursday,
            IsFriday = source.IsFriday,
            IsSaturday = source.IsSaturday,
            IsSunday = source.IsSunday,
            IsHoliday = source.IsHoliday,
            IsWeekdayAndHoliday = source.IsWeekdayAndHoliday,
            IsTimeRange = source.IsTimeRange,
            IsSporadic = source.IsSporadic,
            SporadicScope = source.SporadicScope,
            Groups = groupResources
        };
    }
}
