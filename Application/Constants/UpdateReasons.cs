// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

public static class UpdateReasons
{
    public const string NoManifest = "No update manifest is available.";
    public const string UpToDate = "Already up to date.";
    public const string RequiresIntermediate = "Current version is below the minimum upgradable version; an intermediate update is required first.";
    public const string OperationInProgress = "An update operation is already in progress.";
    public const string UpdateEnqueued = "Update enqueued.";
    public const string PreviousAttemptFailed = "The last automatic attempt at this version failed; start it manually to try again.";
    public const string NoArtifactForInstallation = "The release provides no signed artifact for this installation type and platform.";
    public const string TargetVersionChanged = "The offered version changed after the confirmation; check again before starting it.";
    public const string NothingToRollback = "No successful update found to roll back.";
    public const string RollbackEnqueued = "Rollback enqueued.";
}
