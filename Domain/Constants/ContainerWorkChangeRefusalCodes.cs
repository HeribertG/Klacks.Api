// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Machine codes of a WorkChange refused on a container work, answered with 400 next to the English detail, so a client
/// can explain the refusal in the user's language. A container is corrected through its inner services and covered
/// through a split, never through a WorkChange of its own.
/// </summary>
public static class ContainerWorkChangeRefusalCodes
{
    /// <summary>A correction, travel or briefing on the container itself.</summary>
    public const string CorrectionRefused = "CONTAINER_CORRECTION_REFUSED";

    /// <summary>A replacement on the container or on one of its inner services; a partial cover of a container runs through a split.</summary>
    public const string ReplacementRefused = "CONTAINER_REPLACEMENT_REFUSED";
}
