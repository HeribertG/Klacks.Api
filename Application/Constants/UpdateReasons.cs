// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

public static class UpdateReasons
{
    public const string NoManifest = "No update manifest is available.";
    public const string UpToDate = "Already up to date.";
    public const string RequiresIntermediate = "Current version is below the minimum upgradable version; an intermediate update is required first.";
    public const string OperationInProgress = "An update operation is already in progress.";
    public const string UpdateEnqueued = "Update enqueued.";
    public const string NothingToRollback = "No successful update found to roll back.";
    public const string RollbackEnqueued = "Rollback enqueued.";
}
