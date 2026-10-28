// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Signs and validates the Playground device token with an IDataProtector of its own purpose. The token is
/// the protected device id (a GUID), so it cannot be forged or edited; any failure to unprotect it means
/// "no valid device", never an exception.
/// </summary>
/// <param name="dataProtectionProvider">Source of the purpose-bound protector</param>

using System.Security.Cryptography;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Playground;
using Microsoft.AspNetCore.DataProtection;

namespace Klacks.Api.Application.Services.Playground;

public class PlaygroundDeviceTokenService : IPlaygroundDeviceTokenService
{
    private readonly IDataProtector _protector;

    public PlaygroundDeviceTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(PlaygroundTokenLimitConstants.DeviceTokenPurpose);
    }

    public string Issue() =>
        _protector.Protect(Guid.NewGuid().ToString(PlaygroundTokenLimitConstants.DeviceIdFormat));

    public string? TryGetDeviceId(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > PlaygroundTokenLimitConstants.MaxDeviceTokenLength)
        {
            return null;
        }

        try
        {
            var payload = _protector.Unprotect(token);
            return Guid.TryParseExact(payload, PlaygroundTokenLimitConstants.DeviceIdFormat, out _) ? payload : null;
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return null;
        }
    }
}
