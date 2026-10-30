// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.DTOs.Update;

/// <summary>
/// The facts the software update advisor decides on.
/// </summary>
/// <param name="Preflight">The trigger's own read-only decision and the facts behind it</param>
/// <param name="SourceConfigured">True when the deployment configures an address for release announcements</param>
/// <param name="LastOperation">The newest Klacks update or rollback, whatever its outcome; null when there is none</param>
/// <param name="RunningWork">Planning work in progress in this server process</param>
/// <param name="OtherConnectedUsers">Users other than the caller with an open assistant connection to this server process</param>
/// <param name="NowUtc">The moment of the evaluation</param>
public sealed record SoftwareUpdateFacts(
    UpdateTriggerPreflightResult Preflight,
    bool SourceConfigured,
    UpdateHistory? LastOperation,
    SoftwareUpdateRunningWork RunningWork,
    int OtherConnectedUsers,
    DateTime NowUtc);
