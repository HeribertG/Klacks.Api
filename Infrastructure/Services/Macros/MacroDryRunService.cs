// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Dry-run of a macro switch or undo on real data. Counts the live, non-scenario entries of every holder in scope — the
/// works of the shifts of a cut group, or the breaks of one absence type — and how many of them are sealed (lock level
/// other than None; no recalculation ever touches those), and evaluates the most recent open entries across all holders,
/// each with the current and the new macro of its own holder, on the inputs production builds through
/// <see cref="IMacroDataProvider"/>, which only reads. A work is evaluated with its stored working time; production
/// recomputes it from start and end right before, which yields the same value unless the stored one is stale. A holder
/// whose current and new macro are equal is counted and sampled but never changes. Nothing is written, and the dry-run's
/// own queries are untracked (<see cref="MacroDryRunQueries"/>); whether the input providers track what they read is their
/// own concern (they never write). Every macro is compiled once through <see cref="MacroScriptRunner"/> and run under a
/// time budget linked with the caller's token, its output is read by <see cref="MacroResultAggregator"/>, and work values
/// pass through <see cref="MacroRateModeAdjuster"/> as in production; a break value is the macro result, and a break whose
/// duration was recorded directly keeps it, as in production. Break inputs are built without a payment interval, as the
/// period recalculation does. Not simulated: overtime stacking of works (it depends on the overtime state of the whole
/// period), unpaid breaks inside containers (they use the parent shift's macro) and work changes. A new macro that no
/// longer exists or does not compile is reported instead of sampled; a current macro that no longer exists or does not
/// compile leaves the current value empty. A cancellation by the caller throws OperationCanceledException; an exhausted
/// budget returns the samples evaluated so far, flagged.
/// </summary>
/// <param name="context">Read access to works, breaks and macro scripts</param>
/// <param name="macroDataProvider">Builds the production macro inputs of a work or a break</param>
/// <param name="budget">Wall-clock budget for building the inputs of all samples and evaluating them; counting, loading
/// the sample entries and compiling run before it starts</param>
/// <param name="holders">The holders in scope, one entry per holder id, each with its current and its new macro</param>

using System.Globalization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Scripting;
using Klacks.Api.Infrastructure.Services.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Macros;

public class MacroDryRunService : IMacroDryRunService
{
    public const int SampleSize = 20;

    private const int DefaultBudgetMs = 10000;
    private const string MacroMissingMessage = "the macro no longer exists.";
    private const string MacroDoesNotCompileMessage = "its script does not compile: {0}";

    private readonly DataBaseContext _context;
    private readonly IMacroDataProvider _macroDataProvider;
    private readonly TimeSpan _budget;

    public MacroDryRunService(DataBaseContext context, IMacroDataProvider macroDataProvider)
        : this(context, macroDataProvider, TimeSpan.FromMilliseconds(DefaultBudgetMs))
    {
    }

    internal MacroDryRunService(DataBaseContext context, IMacroDataProvider macroDataProvider, TimeSpan budget)
    {
        _context = context;
        _macroDataProvider = macroDataProvider;
        _budget = budget;
    }

    public async Task<MacroDryRunResult> RunAsync(
        MacroAssignmentTarget target,
        IReadOnlyList<MacroDryRunHolder> holders,
        CancellationToken cancellationToken = default)
    {
        var holderIds = holders.Select(holder => holder.HolderId).Distinct().ToList();
        var (total, sealedCount) = await CountAsync(target, holderIds, cancellationToken);
        var scripts = new Dictionary<Guid, CompiledScript?>();
        foreach (var macroId in holders.Select(holder => holder.NewMacroId).OfType<Guid>().Distinct())
        {
            var (script, error) = await CompileAsync(macroId, cancellationToken);
            if (error != null)
            {
                return MacroDryRunResult.NewMacroFailed(total, sealedCount, error);
            }

            scripts[macroId] = script;
        }

        foreach (var macroId in holders.Select(holder => holder.CurrentMacroId).OfType<Guid>().Distinct())
        {
            if (!scripts.ContainsKey(macroId))
            {
                scripts[macroId] = (await CompileAsync(macroId, cancellationToken)).Script;
            }
        }

        var byHolder = holders.ToDictionary(holder => holder.HolderId);
        var samples = new List<MacroDryRunSample>();
        var exceeded = target == MacroAssignmentTarget.Shift
            ? await SampleWorksAsync(holderIds, byHolder, scripts, samples, cancellationToken)
            : await SampleBreaksAsync(holderIds, byHolder, scripts, samples, cancellationToken);

        return new MacroDryRunResult(total, sealedCount, samples, null, exceeded);
    }

    private async Task<(int Total, int Sealed)> CountAsync(
        MacroAssignmentTarget target, IReadOnlyCollection<Guid> holderIds, CancellationToken cancellationToken)
    {
        if (target == MacroAssignmentTarget.Shift)
        {
            return (
                await MacroDryRunQueries.WorksOfShifts(_context, holderIds).CountAsync(cancellationToken),
                await MacroDryRunQueries.SealedWorksOfShifts(_context, holderIds).CountAsync(cancellationToken));
        }

        return (
            await MacroDryRunQueries.BreaksOfAbsenceTypes(_context, holderIds).CountAsync(cancellationToken),
            await MacroDryRunQueries.SealedBreaksOfAbsenceTypes(_context, holderIds).CountAsync(cancellationToken));
    }

    private async Task<(CompiledScript? Script, string? Error)> CompileAsync(
        Guid macroId, CancellationToken cancellationToken)
    {
        var content = await MacroDryRunQueries.MacroContent(_context, macroId).FirstOrDefaultAsync(cancellationToken);
        if (content == null)
        {
            return (null, MacroMissingMessage);
        }

        var (script, error) = MacroScriptRunner.TryCompile(content);
        return script == null
            ? (null, string.Format(CultureInfo.InvariantCulture, MacroDoesNotCompileMessage, error))
            : (script, null);
    }

    private async Task<bool> SampleWorksAsync(
        IReadOnlyCollection<Guid> holderIds,
        IReadOnlyDictionary<Guid, MacroDryRunHolder> byHolder,
        IReadOnlyDictionary<Guid, CompiledScript?> scripts,
        List<MacroDryRunSample> samples,
        CancellationToken callerToken)
    {
        var works = await MacroDryRunQueries.OpenWorkSamples(_context, holderIds, SampleSize).ToListAsync(callerToken);
        using var budget = StartBudget(callerToken);
        foreach (var work in works)
        {
            var holder = byHolder[work.ShiftId];
            var data = await _macroDataProvider.GetMacroDataAsync(work);
            var currentValue = Evaluate(ScriptOf(scripts, holder.CurrentMacroId), data, true, budget.Token);
            var newValue = Evaluate(ScriptOf(scripts, holder.NewMacroId), data, true, budget.Token);
            if (budget.IsCancellationRequested)
            {
                callerToken.ThrowIfCancellationRequested();
                return true;
            }

            samples.Add(new MacroDryRunSample(work.Id, work.CurrentDate, work.Surcharges, currentValue, newValue, false));
        }

        return false;
    }

    private async Task<bool> SampleBreaksAsync(
        IReadOnlyCollection<Guid> holderIds,
        IReadOnlyDictionary<Guid, MacroDryRunHolder> byHolder,
        IReadOnlyDictionary<Guid, CompiledScript?> scripts,
        List<MacroDryRunSample> samples,
        CancellationToken callerToken)
    {
        var breaks = await MacroDryRunQueries.OpenBreakSamples(_context, holderIds, SampleSize).ToListAsync(callerToken);
        using var budget = StartBudget(callerToken);
        foreach (var breakEntry in breaks)
        {
            if (BreakMacroService.HasDirectlyRecordedDuration(breakEntry))
            {
                samples.Add(new MacroDryRunSample(
                    breakEntry.Id, breakEntry.CurrentDate, breakEntry.WorkTime, null, null, true));
                continue;
            }

            var holder = byHolder[breakEntry.AbsenceId];
            var data = await _macroDataProvider.GetMacroDataForBreakAsync(breakEntry);
            var currentValue = Evaluate(ScriptOf(scripts, holder.CurrentMacroId), data, false, budget.Token);
            var newValue = Evaluate(ScriptOf(scripts, holder.NewMacroId), data, false, budget.Token);
            if (budget.IsCancellationRequested)
            {
                callerToken.ThrowIfCancellationRequested();
                return true;
            }

            samples.Add(new MacroDryRunSample(
                breakEntry.Id, breakEntry.CurrentDate, breakEntry.WorkTime, currentValue, newValue, false));
        }

        return false;
    }

    private CancellationTokenSource StartBudget(CancellationToken callerToken)
    {
        var budget = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        budget.CancelAfter(_budget);
        return budget;
    }

    private static CompiledScript? ScriptOf(IReadOnlyDictionary<Guid, CompiledScript?> scripts, Guid? macroId) =>
        macroId.HasValue && scripts.TryGetValue(macroId.Value, out var script) ? script : null;

    private static decimal? Evaluate(CompiledScript? script, MacroData data, bool appliesRateModes, CancellationToken budget)
    {
        if (script == null)
        {
            return null;
        }

        var run = MacroScriptRunner.Run(script, data, budget);
        if (!run.IsCompleted)
        {
            return null;
        }

        var result = MacroResultAggregator.Aggregate(run.Messages!);
        return (appliesRateModes ? MacroRateModeAdjuster.Apply(result, data) : result).ResultValue;
    }
}
