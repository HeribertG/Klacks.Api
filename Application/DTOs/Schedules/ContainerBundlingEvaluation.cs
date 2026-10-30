// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Outcome of advising on a bundling. Facts are kept apart from the findings and the recommendation, which are computed
/// deterministically. With NotCalculated travel times no tour figure and no time-based reason is reported, because they
/// would rest on a straight-line estimate. Nothing is created or stored.
/// </summary>
/// <param name="Verdict">Overall answer</param>
/// <param name="TravelTimes">Whether travel times of the routing service were available</param>
/// <param name="CandidateCount">Mobile services that were looked at</param>
/// <param name="LeftOutCount">Services that stay out; complete even when the list is capped</param>
/// <param name="Route">The tour; null when none can be stated</param>
/// <param name="LeftOut">Services that stay out, capped</param>
/// <param name="Findings">Deterministic findings, blocking ones first</param>
/// <param name="Recommendation">Conservative summary</param>
/// <param name="NotMeasured">What this advice cannot see</param>
public sealed record ContainerBundlingEvaluation(
    ContainerBundlingVerdict Verdict,
    ContainerBundlingTravelTimes TravelTimes,
    int CandidateCount,
    int LeftOutCount,
    ContainerBundlingRouteFacts? Route,
    IReadOnlyList<ContainerBundlingLeftOut> LeftOut,
    IReadOnlyList<ContainerBundlingFinding> Findings,
    string Recommendation,
    string NotMeasured);
