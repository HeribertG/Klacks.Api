// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Compares an original macro with an extended copy on <see cref="MacroRegressionGrid"/>. Channel values are
/// aggregated the way production totals them (channel 1: last value, other channels: sum). Every channel other
/// than 1 that the original emits with a non-zero value must come out identical in the copy; a surcharge channel
/// (10-14) the original leaves at zero or does not emit for that input may carry an added surcharge. The result
/// channel 1 is compared strictly, 0 counting as a real result: when the original emits it, the copy must emit
/// either the same value or the original value plus the sum of the added surcharges of that input; when the
/// original does not emit it, the copy must not emit it either. The sum form alone allows a deviation of at most
/// 0.000001, because the script adds in binary floating point (8.4 + 4.2 comes out as 12.600000000000001)
/// while the expected sum is formed in decimal from the printed values; the unchanged form stays exact. The rule
/// for channel 1 lives in one method (CheckResultChannel). An input on which the original fails is
/// skipped; an input on which only the copy fails aborts the check. Every abort carries a
/// <see cref="MacroRegressionFailureKind"/>, so only a runtime failure of the copy is attributed to the appended
/// script. A compiler crash (a comment on the last line without a line break after it) counts as a compile error
/// of that script and is reported with a fixed message. Inputs are bound through
/// <see cref="MacroDataImportBinder"/>, and execution runs on the calling thread under a cooperative time budget
/// that the interpreter checks after every instruction, so an exhausted budget leaves no thread running. The
/// caller's cancellation token is linked with that budget: a cancellation by the caller (the user stops the turn)
/// throws OperationCanceledException, only the exhausted budget is reported as BudgetExceeded.
/// </summary>
/// <param name="budget">Wall-clock budget for executing both scripts on the whole grid (compilation excluded)</param>
/// <param name="cancellationToken">Cancellation of the caller, for example the stopped assistant turn</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Infrastructure.Scripting;

namespace Klacks.Api.Infrastructure.Services.Macros;

public class MacroRegressionChecker : IMacroRegressionChecker
{
    private const int DefaultBudgetMs = 30000;
    private const int MaxReportedDeviations = 10;
    private const int ResultChannel = (int)MacroTypeEnum.DefaultResult;
    private const decimal AcceptedTotalTolerance = 0.000001m;
    private const string OriginalCompileFailedMessage = "The original macro script does not compile: {0}";
    private const string CopyCompileFailedMessage = "The extended script does not compile: {0}";
    private const string TrailingCommentCompileError =
        "it ends with a comment on its last line without a line break after it, which the script parser cannot handle.";
    private const string CopyRuntimeFailedMessage =
        "The extended script fails at test input [{0}] although the original runs there: {1}";
    private const string BudgetExceededMessage = "The regression check did not finish within {0} ms.";
    private const string NoComparableSampleMessage =
        "The original macro could not be executed on any test input, so its output cannot be compared.";

    private readonly TimeSpan _budget;

    public MacroRegressionChecker()
        : this(TimeSpan.FromMilliseconds(DefaultBudgetMs))
    {
    }

    internal MacroRegressionChecker(TimeSpan budget)
    {
        _budget = budget;
    }

    public MacroRegressionResult Check(
        string originalContent, string copyContent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (original, originalError) = TryCompile(originalContent);
        if (original == null)
        {
            return MacroRegressionResult.Failure(
                MacroRegressionFailureKind.OriginalCompileError,
                Format(OriginalCompileFailedMessage, originalError));
        }

        var (copy, copyError) = TryCompile(copyContent);
        if (copy == null)
        {
            return MacroRegressionResult.Failure(
                MacroRegressionFailureKind.CopyCompileError,
                Format(CopyCompileFailedMessage, copyError));
        }

        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(_budget);
        return CompareOnGrid(original, copy, budget.Token, cancellationToken);
    }

    private static (CompiledScript? Script, string? Error) TryCompile(string content)
    {
        try
        {
            var compiled = CompiledScript.Compile(content);
            return compiled.HasError ? (null, compiled.Error?.Description) : (compiled, null);
        }
        catch (ArgumentOutOfRangeException)
        {
            return (null, TrailingCommentCompileError);
        }
    }

    private MacroRegressionResult CompareOnGrid(
        CompiledScript original, CompiledScript copy, CancellationToken budget, CancellationToken callerToken)
    {
        var deviations = new List<MacroRegressionDeviation>();
        var compared = 0;
        var skipped = 0;

        foreach (var sample in MacroRegressionGrid.Samples)
        {
            var originalValues = Run(original, sample.Data, budget, out _);
            if (budget.IsCancellationRequested)
            {
                callerToken.ThrowIfCancellationRequested();
                return BudgetExceeded();
            }

            if (originalValues == null)
            {
                skipped++;
                continue;
            }

            var copyValues = Run(copy, sample.Data, budget, out var copyError);
            if (budget.IsCancellationRequested)
            {
                callerToken.ThrowIfCancellationRequested();
                return BudgetExceeded();
            }

            if (copyValues == null)
            {
                return MacroRegressionResult.Failure(
                    MacroRegressionFailureKind.CopyRuntimeError,
                    Format(CopyRuntimeFailedMessage, sample.Description, copyError));
            }

            compared++;
            CollectDeviations(sample, originalValues, copyValues, deviations);
        }

        if (compared == 0)
        {
            return MacroRegressionResult.Failure(MacroRegressionFailureKind.NoComparableSample, NoComparableSampleMessage);
        }

        return new MacroRegressionResult(
            compared,
            skipped,
            deviations.Take(MaxReportedDeviations).ToList(),
            deviations.Count,
            null);
    }

    private MacroRegressionResult BudgetExceeded() =>
        MacroRegressionResult.Failure(
            MacroRegressionFailureKind.BudgetExceeded,
            Format(BudgetExceededMessage, (int)_budget.TotalMilliseconds));

    private static Dictionary<int, decimal>? Run(
        CompiledScript compiled, MacroData data, CancellationToken budget, out string? error)
    {
        try
        {
            var script = compiled.CloneForExecution();
            MacroDataImportBinder.Bind(script, data);
            var result = new ScriptExecutionContext(script).Execute(budget);
            error = result.Error?.Description;
            return result.Success ? AggregateChannels(result.Messages) : null;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    private static Dictionary<int, decimal> AggregateChannels(IEnumerable<ResultMessage> messages)
    {
        var values = new Dictionary<int, decimal>();
        foreach (var message in messages)
        {
            if (!decimal.TryParse(message.Message, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                continue;
            }

            values[message.Type] = message.Type == ResultChannel
                ? parsed
                : values.GetValueOrDefault(message.Type) + parsed;
        }

        return values;
    }

    private static void CollectDeviations(
        MacroRegressionSample sample,
        IReadOnlyDictionary<int, decimal> originalValues,
        IReadOnlyDictionary<int, decimal> copyValues,
        List<MacroRegressionDeviation> deviations)
    {
        var resultDeviation = CheckResultChannel(sample, originalValues, copyValues);
        if (resultDeviation != null)
        {
            deviations.Add(resultDeviation);
        }

        foreach (var (channel, originalValue) in originalValues)
        {
            if (channel == ResultChannel)
            {
                continue;
            }

            var deviation = CheckUnchanged(sample, channel, originalValue, copyValues);
            if (deviation != null)
            {
                deviations.Add(deviation);
            }
        }
    }

    private static MacroRegressionDeviation? CheckResultChannel(
        MacroRegressionSample sample,
        IReadOnlyDictionary<int, decimal> originalValues,
        IReadOnlyDictionary<int, decimal> copyValues)
    {
        var hasCopyResult = copyValues.TryGetValue(ResultChannel, out var copyResult);
        if (!originalValues.TryGetValue(ResultChannel, out var originalResult))
        {
            return hasCopyResult
                ? new MacroRegressionDeviation(sample.Description, ResultChannel, null, copyResult)
                : null;
        }

        if (hasCopyResult && copyResult == originalResult)
        {
            return null;
        }

        var acceptedTotal = originalResult + AddedSurchargeTotal(originalValues, copyValues);
        if (hasCopyResult && acceptedTotal.HasValue && Math.Abs(copyResult - acceptedTotal.Value) <= AcceptedTotalTolerance)
        {
            return null;
        }

        return new MacroRegressionDeviation(
            sample.Description, ResultChannel, originalResult, hasCopyResult ? copyResult : null, acceptedTotal);
    }

    private static decimal? AddedSurchargeTotal(
        IReadOnlyDictionary<int, decimal> originalValues,
        IReadOnlyDictionary<int, decimal> copyValues)
    {
        var added = MacroOutputChannels.Surcharges
            .Where(channel => originalValues.GetValueOrDefault(channel) == 0m)
            .Select(channel => copyValues.GetValueOrDefault(channel))
            .Where(value => value != 0m)
            .ToList();

        return added.Count == 0 ? null : added.Sum();
    }

    private static MacroRegressionDeviation? CheckUnchanged(
        MacroRegressionSample sample,
        int channel,
        decimal originalValue,
        IReadOnlyDictionary<int, decimal> copyValues)
    {
        if (originalValue == 0m)
        {
            return null;
        }

        var hasCopyValue = copyValues.TryGetValue(channel, out var copyValue);
        if (hasCopyValue && copyValue == originalValue)
        {
            return null;
        }

        return new MacroRegressionDeviation(sample.Description, channel, originalValue, hasCopyValue ? copyValue : null);
    }

    private static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);
}
