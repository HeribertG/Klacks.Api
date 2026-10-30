// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Update;

/// <summary>
/// The result of evaluate_software_update: countable facts, the findings and the server-computed verdict and
/// recommendation, which the model relays and does not rephrase into a different conclusion.
/// </summary>
/// <param name="CurrentVersion">The running version</param>
/// <param name="LatestVersion">The newest released version of the channel; null when no release information could be read</param>
/// <param name="Channel">The configured release channel</param>
/// <param name="Installation">Docker or Native</param>
/// <param name="CanStart">True when the trigger would enqueue the update right now</param>
/// <param name="RunningWork">Planning work in progress in this server process</param>
/// <param name="OtherConnectedUsers">Users other than the caller with an open assistant connection to this server process</param>
/// <param name="Verdict">The computed verdict</param>
/// <param name="Findings">Every finding, blocking ones first</param>
/// <param name="Recommendation">The computed recommendation</param>
/// <param name="NotMeasured">What this advice cannot see</param>
public sealed record SoftwareUpdateEvaluation(
    string CurrentVersion,
    string? LatestVersion,
    string Channel,
    string Installation,
    bool CanStart,
    SoftwareUpdateRunningWork RunningWork,
    int OtherConnectedUsers,
    SoftwareUpdateVerdict Verdict,
    IReadOnlyList<SoftwareUpdateFinding> Findings,
    string Recommendation,
    string NotMeasured);
