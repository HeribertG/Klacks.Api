// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Playground;

/// <summary>
/// Issues and validates the signed device token that identifies one Playground browser. A token that is
/// missing, malformed or tampered with is simply invalid; validation never throws.
/// </summary>
public interface IPlaygroundDeviceTokenService
{
    /// <summary>
    /// Mints a new token for a fresh device id.
    /// </summary>
    string Issue();

    /// <summary>
    /// Returns the device id inside a genuine token, null when the token is absent, oversized, malformed or tampered with.
    /// </summary>
    /// <param name="token">Raw value of the X-Playground-Device header</param>
    string? TryGetDeviceId(string? token);
}
