// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deterministic advice on recording an employee's exit at a date: whether the recording skill would refuse it (no
/// active membership, several active memberships, exit before the membership start), what stays booked after the exit
/// (work, absences, on-call duty, entries on approved or closed days), contracts that do not end on the exit date and
/// group memberships that remain. Pure and static, so every rule is pinned by a unit test; the model only relays it.
/// Ending the membership removes nothing that is already booked: the entries stay in the plan until somebody removes or
/// reassigns them, and only new bookings after the exit date are refused.
/// </summary>
/// <param name="clientId">Identifier of the employee the advice is about</param>
/// <param name="facts">The gathered facts about the exit</param>

using Klacks.Api.Application.DTOs.Clients;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Clients;

public static class EmployeeExitAdvisor
{
    private const string ConfirmationNotice = "Nothing has been changed; the system asks for one confirmation before the exit is recorded.";
    private const string BookedEntriesNotice = "Entries that are already booked after the exit date are not removed or reassigned by the exit; they stay in the plan until somebody deals with them, and only new bookings after that day are refused.";

    private static readonly HashSet<EmployeeExitFindingCode> DecisionCodes =
    [
        EmployeeExitFindingCode.SealedEntriesAfterExit,
        EmployeeExitFindingCode.FuturePlanAfterExit,
        EmployeeExitFindingCode.AbsencesAfterExit,
        EmployeeExitFindingCode.OnCallAfterExit,
        EmployeeExitFindingCode.ContractRunsPastExit,
        EmployeeExitFindingCode.ContractEndedBeforeExit
    ];

    public static EmployeeExitEvaluation Evaluate(Guid clientId, EmployeeExitFacts facts)
    {
        var findings = Analyse(facts);

        return new EmployeeExitEvaluation(clientId, facts, findings, BuildRecommendation(facts, findings));
    }

    private static List<EmployeeExitFinding> Analyse(EmployeeExitFacts facts)
    {
        var findings = new List<EmployeeExitFinding>();
        AddMembershipFindings(facts, findings);
        AddPlanFindings(facts, findings);
        AddContractFindings(facts, findings);

        if (facts.GroupCount > 0)
        {
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.GroupMembershipsRemain,
                $"{facts.EmployeeName} has group entries in {facts.GroupCount} group(s) (their validity was not checked); the exit does not change them, planning simply stops at the end of the employment.",
                false));
        }

        return findings.OrderBy(finding => !finding.IsBlocking).ToList();
    }

    private static void AddMembershipFindings(EmployeeExitFacts facts, List<EmployeeExitFinding> findings)
    {
        var membership = facts.Membership;
        var name = facts.EmployeeName;
        var exit = Format(facts.ExitDate);

        if (membership.ActiveCount == 0)
        {
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.NoActiveMembership,
                $"{name} has no active membership, so there is nothing to end.",
                true));
            return;
        }

        if (membership.ActiveCount > 1)
        {
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.MultipleActiveMemberships,
                $"{name} has {membership.ActiveCount} active memberships and the exit cannot decide which one to end: {membership.Options}.",
                true));
            return;
        }

        if (membership.ValidFrom.HasValue && facts.ExitDate < membership.ValidFrom.Value)
        {
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.ExitBeforeMembershipStart,
                $"The exit date {exit} lies before the start of the membership ({Format(membership.ValidFrom.Value)}).",
                true));
        }
        else if (membership.ValidUntil == facts.ExitDate)
        {
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.MembershipAlreadyEndsOnExitDate,
                $"The membership of {name} already ends on {exit}; recording the exit would change nothing.",
                false));
        }
    }

    private static void AddPlanFindings(EmployeeExitFacts facts, List<EmployeeExitFinding> findings)
    {
        var plan = facts.Plan;
        var name = facts.EmployeeName;
        var searched = $"between {Format(facts.ExitDate.AddDays(1))} and {Format(facts.HorizonEnd)}";

        if (plan.ClosedEntriesAfterExit > 0 || plan.ApprovedEntriesAfterExit > 0)
        {
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.SealedEntriesAfterExit,
                $"{name} has {plan.ClosedEntriesAfterExit} entry(ies) on closed periods and {plan.ApprovedEntriesAfterExit} on approved days {searched}; " +
                "closed entries cannot be removed or reassigned until the period is reopened, approved ones only by an administrator or supervisor.",
                false));
        }

        if (plan.WorksAfterExit > 0)
        {
            var past = plan.WorksAlreadyPast > 0 ? $", {plan.WorksAlreadyPast} of them on days that have already passed" : string.Empty;
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.FuturePlanAfterExit,
                $"{name} is booked for {plan.WorksAfterExit} work entry(ies) {searched} (first {Format(plan.FirstWorkDate)}, last {Format(plan.LastWorkDate)}){past}.",
                false));
        }

        if (plan.AbsenceDaysAfterExit > 0)
        {
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.AbsencesAfterExit,
                $"{name} has absences on {plan.AbsenceDaysAfterExit} day(s) {searched}.",
                false));
        }

        if (plan.OnCallDaysAfterExit > 0)
        {
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.OnCallAfterExit,
                $"{name} is on call on {plan.OnCallDaysAfterExit} day(s) {searched}.",
                false));
        }
    }

    private static void AddContractFindings(EmployeeExitFacts facts, List<EmployeeExitFinding> findings)
    {
        var contracts = facts.Contracts;
        var name = facts.EmployeeName;
        var exit = Format(facts.ExitDate);

        if (contracts.RunningPastExit > 0)
        {
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.ContractRunsPastExit,
                $"{contracts.RunningPastExit} contract assignment(s) of {name} are open-ended or run past {exit}; the exit does not end them.",
                false));
        }
        else if (!contracts.CoveredOnExitDate && contracts.LatestEndBeforeExit.HasValue)
        {
            findings.Add(new EmployeeExitFinding(
                EmployeeExitFindingCode.ContractEndedBeforeExit,
                $"The last contract assignment of {name} ended on {Format(contracts.LatestEndBeforeExit)}, before the exit date {exit}.",
                false));
        }
    }

    private static string BuildRecommendation(EmployeeExitFacts facts, IReadOnlyList<EmployeeExitFinding> findings)
    {
        var name = facts.EmployeeName;
        var exit = Format(facts.ExitDate);

        var blocking = findings.FirstOrDefault(finding => finding.IsBlocking);
        if (blocking != null)
        {
            return $"Do not record the exit of {name} on {exit} as asked: {blocking.Statement} {ConfirmationNotice}";
        }

        if (findings.Any(finding => finding.Code == EmployeeExitFindingCode.MembershipAlreadyEndsOnExitDate))
        {
            return $"The membership of {name} already ends on {exit}; nothing would change. {ConfirmationNotice}";
        }

        var firstDayOut = Format(facts.ExitDate.AddDays(1));
        var decisions = findings.Where(finding => DecisionCodes.Contains(finding.Code)).ToList();
        if (decisions.Count == 0)
        {
            return $"Recording the exit of {name} on {exit} leaves nothing booked after that day up to {Format(facts.HorizonEnd)}; planning stops from {firstDayOut}. {ConfirmationNotice}";
        }

        var more = decisions.Count - 1;
        var moreText = more > 0 ? $" {more} further point(s) are listed." : string.Empty;

        return $"Recording the exit of {name} on {exit} stops planning from {firstDayOut}, but {decisions.Count} point(s) need a decision about what stays booked. {decisions[0].Statement}{moreText} {BookedEntriesNotice} {ConfirmationNotice}";
    }

    private static string Format(DateOnly date) => date.ToString(DateTimeFormats.IsoDate);

    private static string Format(DateOnly? date) => date.HasValue ? Format(date.Value) : string.Empty;
}
