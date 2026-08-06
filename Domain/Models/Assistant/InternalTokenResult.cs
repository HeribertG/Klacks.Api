// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of minting a short-lived token for a background path. A refusal carries a reason phrased for
/// the owner, because the caller usually has to tell them why their scheduled work stopped running.
/// </summary>
/// <param name="Success">True when a token was issued</param>
/// <param name="Token">The minted token; null on refusal</param>
/// <param name="Roles">The roles the token was issued with, after any ceiling was applied. Callers
/// expand these instead of reading a permission set frozen when the work was scheduled.</param>
/// <param name="Reason">Why no token was issued; null on success</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record InternalTokenResult(
    bool Success,
    BearerToken? Token,
    IReadOnlyList<string> Roles,
    string? Reason)
{
    public static InternalTokenResult Issued(BearerToken token, IReadOnlyList<string> roles) =>
        new(true, token, roles, null);

    public static InternalTokenResult Refused(string reason) =>
        new(false, null, Array.Empty<string>(), reason);
}
