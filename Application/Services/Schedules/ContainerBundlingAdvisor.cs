// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deterministic advice on whether mobile services fit one tour, derived only from the dry run of the real bundling
/// calculation. Pure and static, so every rule is pinned by a unit test; the model only relays the result. When the
/// routing service gave no travel times nothing is claimed to fit or not to fit: the verdict says so and every figure that
/// rests on the estimate stays out. Time and distance only, no pay or cost statement.
/// </summary>
/// <param name="facts">What the dry run found</param>

using System.Globalization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerBundlingAdvisor
{
    public const string NotMeasured =
        "The route order comes from a randomized optimizer, so with more than two stops the real run can differ in order " +
        "and selection. Not looked at: services already used in another container (they are no candidates), whether a break " +
        "at a fixed time collides with a visit, and staff availability or qualifications. A user with limited group " +
        "visibility sees the round built from the services visible to them, so the real run can differ.";

    private const string NothingChangedNotice = "Nothing has been created or stored; creating the container is a separate step.";

    private static readonly ContainerBundlingLeftOutReason[] TimeBasedReasons =
    [
        ContainerBundlingLeftOutReason.TooLongForTheDay,
        ContainerBundlingLeftOutReason.TimeWindowConflict,
        ContainerBundlingLeftOutReason.NoRoomOnTour,
    ];

    public static ContainerBundlingEvaluation Evaluate(ContainerBundlingFacts facts)
    {
        var leftOut = facts.TravelTimesEstimated
            ? facts.LeftOut.Where(entry => !TimeBasedReasons.Contains(entry.Reason)).ToList()
            : facts.LeftOut.ToList();
        var verdict = DecideVerdict(facts);
        var findings = Analyse(facts, verdict, leftOut);

        return new ContainerBundlingEvaluation(
            verdict,
            HasCalculatedTravelTimes(facts) ? ContainerBundlingTravelTimes.Calculated : ContainerBundlingTravelTimes.NotCalculated,
            facts.CandidateCount,
            leftOut.Count,
            facts.TravelTimesEstimated ? null : facts.Route,
            leftOut.Take(ContainerBundlingAdviceLimits.MaxListedLeftOut).ToList(),
            findings.OrderByDescending(finding => finding.IsBlocking).ToList(),
            BuildRecommendation(facts, verdict, leftOut.Count, findings),
            NotMeasured);
    }

    private static bool HasCalculatedTravelTimes(ContainerBundlingFacts facts) =>
        !facts.TravelTimesEstimated
        && facts.Outcome is ContainerAutofillPlanOutcome.Routed or ContainerAutofillPlanOutcome.NothingSelected;

    private static ContainerBundlingVerdict DecideVerdict(ContainerBundlingFacts facts)
    {
        if (facts.CandidateCount == 0)
        {
            return ContainerBundlingVerdict.NoServices;
        }

        return facts.Outcome switch
        {
            ContainerAutofillPlanOutcome.NoTimeRangeTasks => ContainerBundlingVerdict.NoServices,
            ContainerAutofillPlanOutcome.NoLocatableTasks => ContainerBundlingVerdict.NoLocatableServices,
            ContainerAutofillPlanOutcome.BaseNotLocated => ContainerBundlingVerdict.BaseNotFound,
            _ when facts.TravelTimesEstimated => ContainerBundlingVerdict.TravelTimesNotCalculated,
            ContainerAutofillPlanOutcome.NothingSelected => ContainerBundlingVerdict.NothingFits,
            _ => DecideRoutedVerdict(facts),
        };
    }

    private static ContainerBundlingVerdict DecideRoutedVerdict(ContainerBundlingFacts facts)
    {
        if (facts.PlacementError is not null)
        {
            return ContainerBundlingVerdict.VisitsCannotBePlaced;
        }

        if (facts.Route is not { VisitCount: >= ContainerBundlingAdviceLimits.MinVisitsForABundle })
        {
            return ContainerBundlingVerdict.SingleVisitOnly;
        }

        return facts.LeftOut.Count == 0 ? ContainerBundlingVerdict.AllFit : ContainerBundlingVerdict.PartlyFits;
    }

    private static List<ContainerBundlingFinding> Analyse(
        ContainerBundlingFacts facts, ContainerBundlingVerdict verdict, List<ContainerBundlingLeftOut> leftOut)
    {
        var findings = new List<ContainerBundlingFinding>();
        if (GateFinding(facts, verdict) is { } gate)
        {
            findings.Add(gate);
        }

        if (!facts.TravelTimesEstimated && facts.PlacementError is null && facts.Route is { } route)
        {
            AddTourFindings(facts, route, findings);
        }

        AddLeftOutFindings(leftOut, findings);
        return findings;
    }

    private static ContainerBundlingFinding? GateFinding(ContainerBundlingFacts facts, ContainerBundlingVerdict verdict) =>
        verdict switch
        {
            ContainerBundlingVerdict.NoServices => Blocking(
                ContainerBundlingFindingCode.NoServices,
                "No mobile service runs on this weekday in this time frame, or all of them are already used in a container."),
            ContainerBundlingVerdict.NoLocatableServices => Blocking(
                ContainerBundlingFindingCode.NoLocatableServices,
                "None of the services could be placed on the map, so no tour could be calculated."),
            ContainerBundlingVerdict.BaseNotFound => Blocking(
                ContainerBundlingFindingCode.BaseNotFound,
                $"The base '{facts.Location}' could not be located, or the geocoding service did not answer, so no tour was calculated."),
            ContainerBundlingVerdict.TravelTimesNotCalculated => Blocking(
                ContainerBundlingFindingCode.TravelTimesNotCalculated,
                "Travel times were not calculated: the routing service gave none (unreachable, switched off or the request was too large). Straight-line estimates are not reported as a result."),
            ContainerBundlingVerdict.NothingFits => Blocking(
                ContainerBundlingFindingCode.NothingFits,
                "No service fits this time frame from this base, so the bundling would create no container."),
            ContainerBundlingVerdict.VisitsCannotBePlaced => Blocking(
                ContainerBundlingFindingCode.VisitsCannotBePlaced,
                $"The bundling would refuse this tour: {facts.PlacementError}"),
            _ => null,
        };

    private static void AddTourFindings(
        ContainerBundlingFacts facts, ContainerBundlingRouteFacts route, List<ContainerBundlingFinding> findings)
    {
        findings.Add(Info(
            ContainerBundlingFindingCode.ServicesFit,
            $"{Number(route.VisitCount)} of {Number(facts.CandidateCount)} service(s) fit one tour from {facts.FromTime} to {facts.UntilTime}."));
        findings.Add(Info(
            ContainerBundlingFindingCode.TravelTime,
            $"The tour takes {Number(route.TravelMinutes)} minutes of travel and {Number(route.OnSiteMinutes)} minutes on site over {route.DistanceKm.ToString(CultureInfo.InvariantCulture)} km; {Number(route.RemainingMinutes)} minutes of the time frame stay free. Each leg counts at least the configured minimum travel time."));
        findings.Add(route.RemainingMinutes >= facts.BreakMinutes
            ? Info(
                ContainerBundlingFindingCode.BreakFits,
                $"A break of {Number(facts.BreakMinutes)} minutes fits into the free time. The bundling itself plans no break; it has to be added afterwards.")
            : Info(
                ContainerBundlingFindingCode.BreakDoesNotFit,
                $"A break of {Number(facts.BreakMinutes)} minutes does not fit into the {Number(route.RemainingMinutes)} free minutes: with a break a visit would have to go. The bundling itself plans no break."));
    }

    private static void AddLeftOutFindings(List<ContainerBundlingLeftOut> leftOut, List<ContainerBundlingFinding> findings)
    {
        AddCountFinding(leftOut, findings, ContainerBundlingLeftOutReason.TimeWindowConflict, ContainerBundlingFindingCode.TimeWindowConflicts,
            "service(s) have a time window that no arrival on this tour hits.");
        AddCountFinding(leftOut, findings, ContainerBundlingLeftOutReason.NoRoomOnTour, ContainerBundlingFindingCode.NoRoomOnTour,
            "service(s) could be reached within their time window, but the tour has no room left for them.");
        AddCountFinding(leftOut, findings, ContainerBundlingLeftOutReason.TooLongForTheDay, ContainerBundlingFindingCode.TooLongForTheDay,
            "service(s) do not fit the time frame even as a single visit from the base.");
        AddCountFinding(leftOut, findings, ContainerBundlingLeftOutReason.NoAddress, ContainerBundlingFindingCode.MissingAddress,
            "service(s) have a client without an address or city.");
        AddCountFinding(leftOut, findings, ContainerBundlingLeftOutReason.SameAddress, ContainerBundlingFindingCode.SameAddress,
            "service(s) share their address with another service; the calculation takes only one service per address.");
        AddCountFinding(leftOut, findings, ContainerBundlingLeftOutReason.NotLocated, ContainerBundlingFindingCode.MissingCoordinates,
            "service(s) have no coordinates and the geocoding service found none or did not answer.");
        AddCountFinding(leftOut, findings, ContainerBundlingLeftOutReason.NotLookedUp, ContainerBundlingFindingCode.CoordinatesNotLookedUp,
            $"service(s) have no coordinates and were not looked up, because one question looks up at most {Number(ContainerBundlingAdviceLimits.MaxGeocodedAddresses)} addresses.");
    }

    private static void AddCountFinding(
        List<ContainerBundlingLeftOut> leftOut,
        List<ContainerBundlingFinding> findings,
        ContainerBundlingLeftOutReason reason,
        ContainerBundlingFindingCode code,
        string statement)
    {
        var count = leftOut.Count(entry => entry.Reason == reason);
        if (count > 0)
        {
            findings.Add(Info(code, $"{Number(count)} {statement}"));
        }
    }

    private static string BuildRecommendation(
        ContainerBundlingFacts facts, ContainerBundlingVerdict verdict, int leftOutCount, List<ContainerBundlingFinding> findings)
    {
        var summary = verdict switch
        {
            ContainerBundlingVerdict.AllFit =>
                $"All {Number(facts.CandidateCount)} services fit one tour around {facts.Location}, so bundling them is workable.",
            ContainerBundlingVerdict.PartlyFits =>
                $"{Number(facts.Route!.VisitCount)} of {Number(facts.CandidateCount)} services fit one tour around {facts.Location}; {Number(leftOutCount)} stay out, see the findings.",
            ContainerBundlingVerdict.SingleVisitOnly =>
                "Only one visit fits, and a container with one visit is no bundle.",
            ContainerBundlingVerdict.TravelTimesNotCalculated =>
                "Whether these services fit one tour cannot be said now, because the travel times were not calculated. Ask again when the routing service answers.",
            _ => findings.First(finding => finding.IsBlocking).Statement,
        };

        var breakNote = findings.FirstOrDefault(finding => finding.Code == ContainerBundlingFindingCode.BreakDoesNotFit);
        return string.Join(' ', new[] { summary, breakNote?.Statement, NothingChangedNotice }.OfType<string>());
    }

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);

    private static ContainerBundlingFinding Blocking(ContainerBundlingFindingCode code, string statement) => new(code, statement, true);

    private static ContainerBundlingFinding Info(ContainerBundlingFindingCode code, string statement) => new(code, statement, false);
}
