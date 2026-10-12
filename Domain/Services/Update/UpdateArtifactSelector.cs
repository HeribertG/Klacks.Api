// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Picks the artifact an installation may apply from a release manifest. Strict on purpose: a Docker installation only
/// takes the Docker images, a native installation only the bundle of its own runtime identifier, and an artifact
/// without signature is never selected. No match means no update, never a fallback to another platform.
/// </summary>

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Services.Update;

public static class UpdateArtifactSelector
{
    public static SelectedUpdateArtifact? Select(UpdateManifest manifest, InstallationIdentity installation)
    {
        return installation.Kind == InstallationKind.Docker
            ? SelectDocker(manifest)
            : SelectNative(manifest, installation.RuntimeIdentifier);
    }

    /// <summary>
    /// Downgrades an "update available" verdict when the release carries nothing this installation may apply, so the
    /// status card and the automatic update policy never offer an update the trigger would reject.
    /// </summary>
    public static UpdateAvailability RestrictToInstallation(
        UpdateAvailability availability, UpdateManifest manifest, InstallationIdentity installation)
    {
        if (availability.Status == UpdateAvailabilityStatus.UpdateAvailable && Select(manifest, installation) is null)
        {
            availability.Status = UpdateAvailabilityStatus.NotAvailableForInstallation;
        }

        return availability;
    }

    private static SelectedUpdateArtifact? SelectDocker(UpdateManifest manifest)
    {
        var docker = manifest.Artifacts.Docker;
        if (docker is null || string.IsNullOrWhiteSpace(docker.ApiImage))
        {
            return null;
        }

        return new SelectedUpdateArtifact(docker.ApiImage, docker.Sha256, manifest.Signature);
    }

    private static SelectedUpdateArtifact? SelectNative(UpdateManifest manifest, string runtimeIdentifier)
    {
        var bundle = manifest.Artifacts.Native
            .Concat(manifest.Artifacts.OnPremWindows)
            .FirstOrDefault(artifact =>
                string.Equals(artifact.Rid, runtimeIdentifier, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(artifact.BundleUrl)
                && !string.IsNullOrWhiteSpace(artifact.Signature));

        return bundle is null ? null : new SelectedUpdateArtifact(bundle.BundleUrl, bundle.Sha256, bundle.Signature);
    }
}
