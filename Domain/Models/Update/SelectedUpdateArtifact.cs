// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one manifest artifact an installation may apply, with the signature the updater verifies.
/// </summary>
/// <param name="ArtifactRef">Docker API image reference or native bundle URL</param>
/// <param name="Sha256">Digest of the image or hash of the bundle</param>
/// <param name="Signature">RSA-SHA256 signature over version, artifact reference and hash</param>

namespace Klacks.Api.Domain.Models.Update;

public sealed record SelectedUpdateArtifact(string ArtifactRef, string Sha256, string Signature);
