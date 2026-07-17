// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Processes macro calculations for work and WorkChange entries (surcharges, working time). Processing is
/// strictly per-entity and cascade-free: the K3/K4 successor reprocessing for sibling Works in the same
/// overtime basis period is deliberately NOT triggered here, because at this point the triggering Work is
/// only staged in the EF change tracker while the successors' prior-hours sums are read from the database
/// — the cascade must run after the surrounding UnitOfWork has persisted (see OvertimeCascadeService,
/// invoked by the Work command handlers after CompleteAsync).
/// </summary>
/// <param name="shiftRepository">Loads shift data including MacroId</param>
/// <param name="macroDataProvider">Calculates macro input data from Work/WorkChange</param>
/// <param name="macroCompilationService">Compiles and executes macros</param>
/// <param name="overtimeSurchargeCalculator">Computes K3 overtime surcharge portions for a Work</param>
/// <param name="context">Database access for WorkChange-to-Work resolution</param>
/// <param name="logger">Logger for warnings and error messages</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class WorkMacroService : IWorkMacroService
{
    private const int AmountDecimalPlaces = 2;

    private readonly DataBaseContext _context;
    private readonly IShiftRepository _shiftRepository;
    private readonly IMacroDataProvider _macroDataProvider;
    private readonly IMacroCompilationService _macroCompilationService;
    private readonly IOvertimeSurchargeCalculator _overtimeSurchargeCalculator;
    private readonly ILogger<WorkMacroService> _logger;

    public WorkMacroService(
        DataBaseContext context,
        IShiftRepository shiftRepository,
        IMacroDataProvider macroDataProvider,
        IMacroCompilationService macroCompilationService,
        IOvertimeSurchargeCalculator overtimeSurchargeCalculator,
        ILogger<WorkMacroService> logger)
    {
        _context = context;
        _shiftRepository = shiftRepository;
        _macroDataProvider = macroDataProvider;
        _macroCompilationService = macroCompilationService;
        _overtimeSurchargeCalculator = overtimeSurchargeCalculator;
        _logger = logger;
    }

    public async Task ProcessWorkMacroAsync(Work work)
    {
        try
        {
            var shift = await _shiftRepository.Get(work.ShiftId);
            if (shift == null)
            {
                _logger.LogWarning("Shift with ID {ShiftId} not found for Work {WorkId}", work.ShiftId, work.Id);
                CalculateWorkTime(work);
                return;
            }

            CalculateWorkTime(work);

            if (!shift.MacroId.HasValue || shift.MacroId.Value == Guid.Empty)
            {
                return;
            }

            var macroData = await _macroDataProvider.GetMacroDataAsync(work);
            var result = ApplyRateModeAdjustments(
                await _macroCompilationService.CompileAndExecuteAsync(shift.MacroId.Value, macroData),
                macroData);

            var overtime = await _overtimeSurchargeCalculator.CalculateAsync(work);
            var stackingMode = await ResolveStackingModeAsync(shift.MacroId.Value);
            result = ApplyOvertimeStacking(result, overtime, stackingMode);

            if (result.Success && result.ResultValue.HasValue)
            {
                work.Surcharges = result.ResultValue.Value;
                await SurchargeItemSynchronizer.ReplaceAsync(
                    _context,
                    work.SurchargeItems,
                    _context.SurchargeItem.Where(si => si.WorkId == work.Id),
                    result.Surcharges);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing macro for Work {WorkId}", work.Id);
        }
    }

    public async Task ProcessWorkChangeMacroAsync(WorkChange workChange)
    {
        try
        {
            CalculateChangeTime(workChange);

            var work = await _context.Work.FirstOrDefaultAsync(w => w.Id == workChange.WorkId);
            if (work == null)
            {
                _logger.LogWarning("Work with ID {WorkId} not found for WorkChange {WorkChangeId}", workChange.WorkId, workChange.Id);
                return;
            }

            var shift = await _shiftRepository.Get(work.ShiftId);
            if (shift == null)
            {
                _logger.LogWarning("Shift with ID {ShiftId} not found for WorkChange {WorkChangeId}", work.ShiftId, workChange.Id);
                return;
            }

            if (!shift.MacroId.HasValue || shift.MacroId.Value == Guid.Empty)
            {
                return;
            }

            var macroData = await _macroDataProvider.GetMacroDataForWorkChangeAsync(workChange, work);
            var result = ApplyRateModeAdjustments(
                await _macroCompilationService.CompileAndExecuteAsync(shift.MacroId.Value, macroData),
                macroData);

            if (result.Success && result.ResultValue.HasValue)
            {
                workChange.Surcharges = result.ResultValue.Value;
                await SurchargeItemSynchronizer.ReplaceAsync(
                    _context,
                    workChange.SurchargeItems,
                    _context.SurchargeItem.Where(si => si.WorkChangeId == workChange.Id),
                    result.Surcharges);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing macro for WorkChange {WorkChangeId}", workChange.Id);
        }
    }

    /// <summary>
    /// Reinterprets a macro's typed surcharge output according to the configured RateMode per surcharge
    /// type. Macros always compute Amount as SegmentHours * Rate (verified against the built-in "AllShift"
    /// macro); for Multiplier and FixedPerHour this arithmetic already yields the desired result (bonus
    /// hours vs. an absolute currency amount respectively — same formula, different unit), so only
    /// FixedPerShift needs an actual override (flat Rate once, independent of segment duration). The
    /// DefaultResult is adjusted by the same delta so any non-surcharge portion of a custom macro's total
    /// is preserved rather than overwritten.
    /// </summary>
    private static MacroExecutionResult ApplyRateModeAdjustments(MacroExecutionResult result, MacroData macroData)
    {
        if (!result.Success || result.Surcharges.Count == 0)
        {
            return result;
        }

        var adjustedItems = result.Surcharges
            .Select(item => item with { Amount = AdjustAmount(item.Type, item.Amount, macroData) })
            .ToList();

        var delta = adjustedItems.Sum(item => item.Amount) - result.Surcharges.Sum(item => item.Amount);
        var adjustedResultValue = result.ResultValue.HasValue
            ? Math.Round(result.ResultValue.Value + delta, AmountDecimalPlaces)
            : result.ResultValue;

        return new MacroExecutionResult(result.Success, adjustedResultValue, adjustedItems);
    }

    private static decimal AdjustAmount(SurchargeType type, decimal amount, MacroData macroData)
    {
        var (rate, mode, minimumPerHour) = GetRateConfig(type, macroData);

        if (mode == SurchargeRateMode.FixedPerShift)
        {
            return amount == 0m ? 0m : rate;
        }

        if (mode == SurchargeRateMode.Multiplier && minimumPerHour.HasValue && rate != 0m)
        {
            var segmentHours = amount / rate;
            var minimumAmount = minimumPerHour.Value * segmentHours;
            return Math.Max(amount, minimumAmount);
        }

        return amount;
    }

    private static (decimal Rate, SurchargeRateMode Mode, decimal? MinimumPerHour) GetRateConfig(SurchargeType type, MacroData macroData) => type switch
    {
        SurchargeType.Night => (macroData.NightRate, macroData.NightRateMode, macroData.NightMinimumPerHour),
        SurchargeType.Weekend1 => (macroData.WE1Rate, macroData.WE1RateMode, macroData.WE1MinimumPerHour),
        SurchargeType.Weekend2 => (macroData.WE2Rate, macroData.WE2RateMode, macroData.WE2MinimumPerHour),
        SurchargeType.Weekend3 => (macroData.WE3Rate, macroData.WE3RateMode, macroData.WE3MinimumPerHour),
        SurchargeType.Holiday => (macroData.HolidayRate, macroData.HolidayRateMode, macroData.HolidayMinimumPerHour),
        _ => (0m, SurchargeRateMode.Multiplier, null)
    };

    /// <summary>
    /// K4: the stacking mode is a structural property of the macro assigned to the shift, not an
    /// installation-wide setting — a shift computed by the StandardAdditive macro stacks additively while
    /// a sibling shift on the plain Standard macro keeps highest-wins, so mixed operation per shift is
    /// possible. Custom macros behave like Standard (highest wins).
    /// </summary>
    private async Task<SurchargeStackingMode> ResolveStackingModeAsync(Guid macroId)
    {
        var function = await _context.Macro
            .Where(m => m.Id == macroId)
            .Select(m => (MacroFunctionEnum)m.Type)
            .FirstOrDefaultAsync();

        return function == MacroFunctionEnum.StandardAdditive
            ? SurchargeStackingMode.Additive
            : SurchargeStackingMode.HighestWins;
    }

    /// <summary>
    /// K4: combines the macro's own (rate-adjusted) surcharge result with the K3 overtime portions per
    /// the stacking mode derived from the shift's macro function. HighestWins (default, and the only
    /// outcome for every installation that never configures overtime — overtime.Items is empty then)
    /// compares the surcharge-only totals — NOT ResultValue itself, which for some macros (e.g. the
    /// seeded "Accident" macro) carries a non-surcharge quantity such as passthrough hours — and, if
    /// overtime wins, replaces only the surcharge portion via the same delta approach
    /// ApplyRateModeAdjustments uses, so a non-surcharge ResultValue component is never clobbered.
    /// Additive always adds both totals and concatenates both item lists.
    /// </summary>
    private static MacroExecutionResult ApplyOvertimeStacking(
        MacroExecutionResult result,
        OvertimeCalculationResult overtime,
        SurchargeStackingMode stackingMode)
    {
        if (!result.Success || overtime.Items.Count == 0)
        {
            return result;
        }

        var overtimeTotal = Math.Round(overtime.Items.Sum(item => item.Amount), AmountDecimalPlaces);

        if (stackingMode == SurchargeStackingMode.Additive)
        {
            var combinedItems = result.Surcharges.Concat(overtime.Items).ToList();
            var combinedResultValue = result.ResultValue.HasValue
                ? Math.Round(result.ResultValue.Value + overtimeTotal, AmountDecimalPlaces)
                : overtimeTotal;
            return new MacroExecutionResult(true, combinedResultValue, combinedItems);
        }

        var existingSurchargeTotal = Math.Round(result.Surcharges.Sum(item => item.Amount), AmountDecimalPlaces);
        if (overtimeTotal <= existingSurchargeTotal)
        {
            return result;
        }

        var delta = overtimeTotal - existingSurchargeTotal;
        var adjustedResultValue = result.ResultValue.HasValue
            ? Math.Round(result.ResultValue.Value + delta, AmountDecimalPlaces)
            : overtimeTotal;

        return new MacroExecutionResult(true, adjustedResultValue, overtime.Items.ToList());
    }

    private static void CalculateWorkTime(Work work)
    {
        var start = work.StartTime.ToTimeSpan();
        var end = work.EndTime.ToTimeSpan();

        TimeSpan duration;
        if (end >= start)
        {
            duration = end - start;
        }
        else
        {
            duration = TimeSpan.FromHours(24) - start + end;
        }

        work.WorkTime = (decimal)duration.TotalHours;
    }

    /// <summary>
    /// Derives ChangeTime from the WorkChange's own Start/End window for Within-style types.
    /// </summary>
    /// <remarks>
    /// TravelWithin and ReplacementWithin carry their own Von/Bis and derive ChangeTime from it (over-midnight via the 24h wrap below). OnCallPresence/OnCallStandby follow the same contract: Start/End are the authoritative on-call window, ChangeTime is the raw duration in hours.
    /// </remarks>
    private static void CalculateChangeTime(WorkChange workChange)
    {
        if (workChange.Type != WorkChangeType.TravelWithin
            && workChange.Type != WorkChangeType.ReplacementWithin
            && workChange.Type != WorkChangeType.OnCallPresence
            && workChange.Type != WorkChangeType.OnCallStandby)
        {
            return;
        }

        var start = workChange.StartTime.ToTimeSpan();
        var end = workChange.EndTime.ToTimeSpan();

        TimeSpan duration;
        if (end >= start)
        {
            duration = end - start;
        }
        else
        {
            duration = TimeSpan.FromHours(24) - start + end;
        }

        workChange.ChangeTime = (decimal)duration.TotalHours;
    }

}
