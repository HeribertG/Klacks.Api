// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Picks the PostgreSQL engine package an installation may use for the engine upgrade. Strict like UpdateArtifactSelector:
/// a Docker installation only takes the engine image, a native installation only the bundle of its own runtime identifier,
/// and a package without reference, hash or signature is never selected. The fields must also pass the format rules the
/// updater applies before it verifies the signature (SignedFieldFormat and EngineUpgradePrecheck in Klacks.Updater): a
/// major of one to three digits, a reference without whitespace, a Docker digest as sha256: plus 64 lowercase hex, a bundle
/// hash as 64 lowercase hex, and no control character anywhere; a malformed package counts as no package, so the card
/// never offers a row the updater would reject. Reference, hash and signature are passed on verbatim (the Docker digest
/// keeps its sha256: prefix), because the signature covers the exact strings. No match means no engine upgrade, never a
/// fallback to another platform.
/// </summary>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Services.Update;

public static partial class EngineUpgradeArtifactSelector
{
    private const int MinEngineMajor = 1;
    private const int MaxEngineMajor = 999;

    public static SelectedUpdateArtifact? Select(PostgresEngineArtifact? engine, InstallationIdentity installation)
    {
        if (engine is null || engine.Major < MinEngineMajor || engine.Major > MaxEngineMajor)
        {
            return null;
        }

        return installation.Kind == InstallationKind.Docker
            ? SelectDocker(engine.Docker)
            : SelectNative(engine.Native ?? [], installation.RuntimeIdentifier);
    }

    private static SelectedUpdateArtifact? SelectDocker(PostgresEngineDockerArtifact? docker)
    {
        if (docker is null
            || !IsWellFormed(docker.Image, docker.Signature)
            || !DockerDigestPattern().IsMatch(docker.Sha256 ?? string.Empty))
        {
            return null;
        }

        return new SelectedUpdateArtifact(docker.Image, docker.Sha256, docker.Signature);
    }

    private static SelectedUpdateArtifact? SelectNative(IEnumerable<OnPremArtifact> bundles, string runtimeIdentifier)
    {
        var bundle = bundles.FirstOrDefault(artifact =>
            string.Equals(artifact.Rid, runtimeIdentifier, StringComparison.OrdinalIgnoreCase));

        if (bundle is null
            || !IsWellFormed(bundle.BundleUrl, bundle.Signature)
            || !HexDigestPattern().IsMatch(bundle.Sha256 ?? string.Empty))
        {
            return null;
        }

        return new SelectedUpdateArtifact(bundle.BundleUrl, bundle.Sha256, bundle.Signature);
    }

    private static bool IsWellFormed(string? artifactRef, string? signature) =>
        !string.IsNullOrWhiteSpace(artifactRef)
        && !artifactRef.Any(char.IsWhiteSpace)
        && !artifactRef.Any(char.IsControl)
        && !string.IsNullOrWhiteSpace(signature)
        && !signature.Any(char.IsControl);

    [GeneratedRegex(@"^sha256:[0-9a-f]{64}\z", RegexOptions.CultureInvariant)]
    private static partial Regex DockerDigestPattern();

    [GeneratedRegex(@"^[0-9a-f]{64}\z", RegexOptions.CultureInvariant)]
    private static partial Regex HexDigestPattern();
}
