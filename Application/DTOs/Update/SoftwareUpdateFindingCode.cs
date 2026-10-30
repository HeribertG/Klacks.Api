// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Update;

/// <summary>
/// The kinds of finding of the software update advisor.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SoftwareUpdateFindingCode>))]
public enum SoftwareUpdateFindingCode
{
    UpdateAvailable = 0,
    AlreadyCurrent = 1,
    UpdateSourceNotConfigured = 2,
    ManifestUnavailable = 3,
    ManifestInvalid = 4,
    IntermediateVersionRequired = 5,
    NoArtifactForInstallation = 6,
    OperationActive = 7,
    OperationNotPickedUp = 8,
    TriggerRefused = 9,
    LastRollbackFailed = 10,
    LastAttemptFailed = 11,
    BetaChannel = 12,
    ContainsMigrations = 13,
    RunningWork = 14,
    OtherUsersConnected = 15
}
