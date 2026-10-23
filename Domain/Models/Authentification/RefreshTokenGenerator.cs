// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Security.Cryptography;

namespace Klacks.Api.Domain.Models.Authentification;

public class RefreshTokenGenerator
{
    private const int RefreshTokenByteLength = 32;

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[RefreshTokenByteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
