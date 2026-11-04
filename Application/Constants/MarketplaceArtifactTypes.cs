// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Artifact types bound into the signature statement of identity-bound marketplace downloads. The values
/// must equal the ones Klacks.Marketplace signs with (ArtifactSignatureStatement).
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class MarketplaceArtifactTypes
{
    public const string Plugin = "plugin";

    public const string LanguagePack = "language-pack";
}
