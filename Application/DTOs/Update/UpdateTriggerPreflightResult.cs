// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.DTOs.Update;

/// <summary>
/// What IUpdateTriggerPreflight found: the facts behind the decision and the decision itself.
/// </summary>
/// <param name="CurrentVersion">The running version</param>
/// <param name="Channel">The configured release channel</param>
/// <param name="Installation">Docker or native, with the runtime identifier</param>
/// <param name="Manifest">The channel manifest; null when none could be read</param>
/// <param name="Availability">The verdict against the manifest, restricted to this installation; null without a manifest</param>
/// <param name="Artifact">The signed artifact of this installation; null when the release has none</param>
/// <param name="ActiveOperation">The active queue row of any type, including third-party operations such as Whisper installs</param>
/// <param name="RejectionReason">The UpdateReasons text the trigger would answer; null when it would enqueue</param>
public sealed record UpdateTriggerPreflightResult(
    SemanticVersion CurrentVersion,
    UpdateChannel Channel,
    InstallationIdentity Installation,
    UpdateManifest? Manifest,
    UpdateAvailability? Availability,
    SelectedUpdateArtifact? Artifact,
    UpdateHistory? ActiveOperation,
    string? RejectionReason);
