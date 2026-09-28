// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class TokenClaimTypes
{
    /// <summary>Marks how a token came to be, so the audit line can tell background work from a person.</summary>
    public const string TokenUse = "token_use";

    /// <summary>Value of <see cref="TokenUse"/> on tokens minted for a background path.</summary>
    public const string InternalTokenUse = "internal";
}
