// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.DTOs.Update;

/// <summary>
/// What IEngineUpgradePreflight found: the facts behind the decision and the decision itself.
/// </summary>
/// <param name="CurrentMajor">Major version of the PostgreSQL server Klacks runs on</param>
/// <param name="AvailableMajor">Engine major the manifest offers; null when it offers none</param>
/// <param name="Artifact">The signed engine package for this installation; null when there is none</param>
/// <param name="BlockedReason">Why the upgrade cannot be queued now; null when it can</param>
public sealed record EngineUpgradePreflightResult(
    int CurrentMajor,
    int? AvailableMajor,
    SelectedUpdateArtifact? Artifact,
    EngineUpgradeBlockedReason? BlockedReason);
