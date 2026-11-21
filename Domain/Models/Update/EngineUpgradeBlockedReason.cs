// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Update;

public enum EngineUpgradeBlockedReason
{
    NoEngineInManifest = 0,
    NoArtifactForInstallation = 1,
    NotNewerThanCurrent = 2,
    OperationInProgress = 3,
    UpdaterCapabilityMissing = 4,
    UpdaterCapabilityStale = 5,
}
