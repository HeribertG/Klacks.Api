// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of comparing the running version against an update manifest.
/// </summary>
namespace Klacks.Api.Domain.Models.Update;

public class UpdateAvailability
{
    public UpdateAvailabilityStatus Status { get; set; }

    public string CurrentVersion { get; set; } = string.Empty;

    public string LatestVersion { get; set; } = string.Empty;

    public UpdateChannel Channel { get; set; }

    public bool ContainsMigrations { get; set; }

    public bool IsUpdateAvailable =>
        Status is UpdateAvailabilityStatus.UpdateAvailable or UpdateAvailabilityStatus.UpdateRequiresIntermediate;
}
