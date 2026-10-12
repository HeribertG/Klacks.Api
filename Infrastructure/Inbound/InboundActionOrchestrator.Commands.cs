// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Planning-command placement of InboundActionOrchestrator: the all-or-nothing seal check before the first write and
/// the placement of every keyword on every range inside one database transaction.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Inbound;

public partial class InboundActionOrchestrator
{
    /// <summary>
    /// Seal check over every day the planning commands would cover, run before the first write, so a sealed day is
    /// reported with its date instead of failing a later range inside the transaction. Uses the same rule as the skill's own guard (global seal or a seal of one of the client's
    /// groups), but over all covered days, including those that already carry the keyword.
    /// </summary>
    /// <param name="clientId">Employee the commands are placed for</param>
    /// <param name="ranges">Date ranges the commands would be placed on</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task<string?> DescribeSealedCommandDaysIssueAsync(
        Guid clientId, IReadOnlyCollection<(DateOnly From, DateOnly Until)> ranges, CancellationToken cancellationToken)
    {
        if (ranges.Any(range => range.Until < range.From))
        {
            return "The announced period ends before it starts, so no planning commands were placed automatically. ";
        }

        var days = ranges.Sum(range => range.Until.DayNumber - range.From.DayNumber + 1);
        if (days > MaxSealCheckRangeDays)
        {
            return $"The announced period spans {days} days (maximum {MaxSealCheckRangeDays}), so the seal check " +
                   "was not performed and no planning commands were placed automatically. ";
        }

        var pairs = InboundCommandDateRanges.Days(ranges)
            .Distinct()
            .Select(day => (day, clientId))
            .ToList();

        var locked = await _sealedDayRepository.GetLockedPairsAsync(pairs, cancellationToken);
        return locked.Count > 0
            ? $"Day {locked.Min(pair => pair.Date):yyyy-MM-dd} of the announced period is sealed, so no planning " +
              "commands were placed automatically (all or nothing). "
            : null;
    }

    /// <summary>
    /// Places every keyword on every range. Runs inside one database transaction (each skill call commits through the
    /// same scoped context), and a failed call throws so the transaction rolls back the ranges already saved.
    /// Returns the number of placed keyword-days.
    /// </summary>
    private async Task<int> PlaceCommandsAsync(
        Guid clientId,
        IReadOnlyCollection<string> keywords,
        IReadOnlyCollection<(DateOnly From, DateOnly Until)> ranges,
        SkillExecutionContext context,
        CancellationToken cancellationToken)
    {
        var placedTotal = 0;
        foreach (var keyword in keywords)
        {
            foreach (var (rangeFrom, rangeUntil) in ranges)
            {
                var result = await _skillExecutor.ExecuteAsync(
                    new SkillInvocation
                    {
                        SkillName = ScheduleCommandsRangeSkill,
                        Parameters = new Dictionary<string, object>
                        {
                            ["clientId"] = clientId,
                            ["fromDate"] = rangeFrom,
                            ["untilDate"] = rangeUntil,
                            ["commandKeyword"] = keyword
                        }
                    },
                    context,
                    cancellationToken);

                if (!result.Success)
                {
                    throw new InboundCommandPlacementFailedException(keyword, result.Message ?? string.Empty);
                }

                placedTotal += rangeUntil.DayNumber - rangeFrom.DayNumber + 1;
            }
        }

        return placedTotal;
    }
}
