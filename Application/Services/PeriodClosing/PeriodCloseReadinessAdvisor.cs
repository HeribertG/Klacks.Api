// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deterministic advice on whether a person should close one period of one group now. Pure and static, so every branch
/// is pinned by a unit test; the model only relays it. The order follows PeriodAutoCloseService: sealed, no own work,
/// not due, errors, close. The gates that bind only the automatic path do not appear: autonomy, close window and tick
/// budget. "Wait" and "nothing to seal" are the policy of the automatic close, not a limit on people: the close handler
/// checks neither the close date nor whether work is left, so a person can close by hand and the text says so, and those
/// findings are not blocking. A reopened or partially sealed period stops Klacksy but not a person (the close handler
/// skips existing day locks), so both are findings and not stops here. Without a stored lag the advice counts from the
/// day after the period end. Open errors are the one real refusal: the close fails while they are open, unless the user
/// saw them and explicitly decides to seal anyway; warnings never gate the close.
/// </summary>
/// <param name="facts">The gathered facts about the period</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.PeriodClosing;

public static class PeriodCloseReadinessAdvisor
{
    public const int MaxListedUnsealedDays = 31;

    private const int LagWhenNoneStored = 0;
    private const string ConfirmationNotice = "Nothing was sealed by this check; closing is a separate step the system confirms once.";
    private const string AutomaticClosePolicyNotice = "This is the rule of the automatic close; a person can still close the period by hand.";

    public static PeriodCloseReadinessEvaluation Evaluate(PeriodCloseReadinessFacts facts)
    {
        var lag = facts.LagDays ?? LagWhenNoneStored;
        var earliestCloseDate = PeriodCloseDateCalculator.FirstAutoCloseDay(facts.PeriodEnd, lag);
        var isDue = PeriodCloseDateCalculator.IsAutoCloseDue(facts.Today, facts.PeriodEnd, lag);
        var verdict = Decide(facts, isDue);
        var findings = Analyse(facts, isDue, earliestCloseDate);

        return new PeriodCloseReadinessEvaluation(
            facts.GroupId,
            facts.GroupName,
            facts.PeriodStart,
            facts.PeriodEnd,
            facts.LagDays,
            earliestCloseDate,
            facts.ErrorCount,
            facts.WarningCount,
            facts.SealedDayCount,
            facts.UnsealedDaysWithWork.Count,
            facts.UnsealedDaysWithWork.Take(MaxListedUnsealedDays).ToList(),
            verdict,
            findings,
            BuildRecommendation(facts, verdict, findings));
    }

    private static PeriodCloseVerdict Decide(PeriodCloseReadinessFacts facts, bool isDue)
    {
        if (facts.Stop is PeriodCloseStop.LastDaySealed or PeriodCloseStop.NoDirectWork)
        {
            return PeriodCloseVerdict.NothingToSeal;
        }

        if (!isDue)
        {
            return PeriodCloseVerdict.WaitUntilDue;
        }

        return facts.ErrorCount > 0 ? PeriodCloseVerdict.FixErrorsFirst : PeriodCloseVerdict.CloseNow;
    }

    private static List<PeriodCloseFinding> Analyse(PeriodCloseReadinessFacts facts, bool isDue, DateOnly earliestCloseDate)
    {
        var findings = new List<PeriodCloseFinding>();
        var label = PeriodLabel(facts);

        if (facts.Stop == PeriodCloseStop.LastDaySealed)
        {
            findings.Add(new PeriodCloseFinding(
                PeriodCloseFindingCode.AlreadySealed, $"{label} is already closed: its last day is sealed.", false));
        }
        else if (facts.Stop == PeriodCloseStop.NoDirectWork)
        {
            findings.Add(NoDirectWorkFinding(facts, label));
        }
        else if (!isDue)
        {
            findings.Add(new PeriodCloseFinding(
                PeriodCloseFindingCode.NotDueYet, $"{label} is not due for the automatic close before {Format(earliestCloseDate)}.", false));
        }

        AddCountFindings(facts, findings);
        return findings;
    }

    private static PeriodCloseFinding NoDirectWorkFinding(PeriodCloseReadinessFacts facts, string label) =>
        facts.HasWorkOnChildGroups
            ? new PeriodCloseFinding(
                PeriodCloseFindingCode.WorkOnlyOnChildGroups,
                $"{label} holds no work on the group's own shifts; the work hangs on its child groups, which are closed on their own.",
                false)
            : new PeriodCloseFinding(
                PeriodCloseFindingCode.NoWorkInPeriod, $"{label} holds no work a seal could act on.", false);

    private static void AddCountFindings(PeriodCloseReadinessFacts facts, List<PeriodCloseFinding> findings)
    {
        if (facts.ErrorCount > 0)
        {
            findings.Add(new PeriodCloseFinding(
                PeriodCloseFindingCode.OpenErrors,
                $"{facts.ErrorCount} error(s) are open; the close is refused until they are fixed or the user, having seen them, explicitly decides to seal anyway.",
                true));
        }

        if (facts.WarningCount > 0)
        {
            findings.Add(new PeriodCloseFinding(
                PeriodCloseFindingCode.Warnings, $"{facts.WarningCount} warning(s) are open; they do not stop the close.", false));
        }

        if (facts.UnsealedDaysWithWork.Count > 0)
        {
            findings.Add(new PeriodCloseFinding(
                PeriodCloseFindingCode.UnsealedDaysWithWork,
                $"{facts.UnsealedDaysWithWork.Count} day(s) with assignments are not sealed yet.",
                false));
        }

        if (facts.SealedDayCount > 0 && facts.Stop != PeriodCloseStop.LastDaySealed)
        {
            findings.Add(new PeriodCloseFinding(
                PeriodCloseFindingCode.PartiallySealed,
                $"{facts.SealedDayCount} day(s) are already sealed; a close keeps them and seals the rest.",
                false));
        }

        if (facts.Stop == PeriodCloseStop.ReopenedBefore)
        {
            findings.Add(new PeriodCloseFinding(
                PeriodCloseFindingCode.ReopenedBefore,
                "Somebody reopened this period before; make sure the correction is finished. Klacksy never closes such a period on its own.",
                false));
        }

        if (facts.LagDays == null)
        {
            findings.Add(new PeriodCloseFinding(
                PeriodCloseFindingCode.NoCloseLagStored,
                "No close delay is stored, so the advice counts from the day after the period end and Klacksy never closes on its own.",
                false));
        }
    }

    private static string BuildRecommendation(
        PeriodCloseReadinessFacts facts, PeriodCloseVerdict verdict, IReadOnlyList<PeriodCloseFinding> findings)
    {
        var lead = findings.Count > 0 ? findings[0].Statement : string.Empty;
        var advice = verdict switch
        {
            PeriodCloseVerdict.NothingToSeal => NothingToSealAdvice(facts, lead),
            PeriodCloseVerdict.WaitUntilDue => $"Wait: {lead}{OpenErrorsHint(facts)} {AutomaticClosePolicyNotice}",
            PeriodCloseVerdict.FixErrorsFirst => $"Do not close yet, fix the {facts.ErrorCount} open error(s) of {PeriodLabel(facts)} first. {lead}",
            _ => $"{PeriodLabel(facts)} is ready to be closed now.{WarningsHint(facts)}"
        };

        return $"{advice} {ConfirmationNotice}";
    }

    private static string NothingToSealAdvice(PeriodCloseReadinessFacts facts, string lead) =>
        facts.Stop == PeriodCloseStop.LastDaySealed && facts.UnsealedDaysWithWork.Count > 0
            ? $"Nothing to seal as a period: {lead} However, {facts.UnsealedDaysWithWork.Count} earlier day(s) with assignments are still unsealed; a close would seal them. {AutomaticClosePolicyNotice}"
            : $"Nothing to seal: {lead} {AutomaticClosePolicyNotice}";

    private static string OpenErrorsHint(PeriodCloseReadinessFacts facts) =>
        facts.ErrorCount > 0 ? $" {facts.ErrorCount} error(s) are already open and can be fixed until then." : string.Empty;

    private static string WarningsHint(PeriodCloseReadinessFacts facts) =>
        facts.WarningCount > 0 ? $" {facts.WarningCount} warning(s) remain, they do not stop the close." : string.Empty;

    private static string PeriodLabel(PeriodCloseReadinessFacts facts) =>
        $"The period {Format(facts.PeriodStart)} to {Format(facts.PeriodEnd)} of group {facts.GroupName}";

    private static string Format(DateOnly date) => date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);
}
