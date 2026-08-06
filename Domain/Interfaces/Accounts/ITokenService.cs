// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces;

public interface ITokenService
{
    /// <param name="user">The account the token identifies</param>
    /// <param name="expires">Absolute expiry</param>
    /// <param name="roles">Role claims to embed; null reads the user's current roles. Pass an explicit
    /// list only to issue a token CAPPED below the user's real roles, never above them.</param>
    /// <param name="additionalClaims">Extra claims, e.g. marking a token as internally minted</param>
    Task<string> CreateToken(
        AppUser user,
        DateTime expires,
        IReadOnlyList<string>? roles = null,
        IReadOnlyDictionary<string, string>? additionalClaims = null);
}
