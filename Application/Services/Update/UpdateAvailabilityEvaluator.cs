// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether an update manifest offers a newer version than the running one and whether
/// that version can be applied directly (current version at or above the manifest's minimum
/// upgradable-from version) or requires an intermediate update first.
/// </summary>
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.Services.Update;

public class UpdateAvailabilityEvaluator : IUpdateAvailabilityEvaluator
{
    public UpdateAvailability Evaluate(SemanticVersion currentVersion, UpdateManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var result = new UpdateAvailability
        {
            CurrentVersion = currentVersion.ToString(),
            LatestVersion = manifest.LatestVersion,
            Channel = manifest.Channel,
            ContainsMigrations = manifest.ContainsMigrations,
        };

        if (!SemanticVersion.TryParse(manifest.LatestVersion, out var latest))
        {
            result.Status = UpdateAvailabilityStatus.ManifestInvalid;
            return result;
        }

        if (latest <= currentVersion)
        {
            result.Status = UpdateAvailabilityStatus.UpToDate;
            return result;
        }

        if (SemanticVersion.TryParse(manifest.MinUpgradableFrom, out var minUpgradableFrom)
            && currentVersion < minUpgradableFrom)
        {
            result.Status = UpdateAvailabilityStatus.UpdateRequiresIntermediate;
            return result;
        }

        result.Status = UpdateAvailabilityStatus.UpdateAvailable;
        return result;
    }
}
