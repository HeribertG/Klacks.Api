// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stable SHA-256 fingerprint of a one-time confirmation token. A record that has to point at one held confirmation
/// for longer than the token lives (the explain-offer counter rows are kept for statistics) stores this fingerprint,
/// never the redeemable token itself; the confirmation store compares fingerprints of its active rows against it.
/// </summary>

using System.Security.Cryptography;
using System.Text;

namespace Klacks.Api.Domain.Security;

public static class ConfirmationTokenHasher
{
    /// <summary>Uppercase hex SHA-256 of the token; the token is a random Guid, so no salt is needed.</summary>
    /// <param name="token">The raw confirmation token</param>
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
