// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent row for a one-time OAuth authorization code issued by the MCP authorization server.
/// Replaces the in-memory code store so the authorize call and the token exchange no longer have to
/// reach the same API instance: with the code in process memory, an exchange routed elsewhere is
/// rejected as invalid_grant even though the client did everything right, and every restart voids
/// all outstanding codes. Deliberately not a <c>BaseEntity</c>: the row is ephemeral and is
/// hard-deleted on redemption or expiry, so a redeemed code cannot be replayed from a leftover row.
/// </summary>

namespace Klacks.Api.Domain.Models.Authentification;

public sealed class OAuthAuthorizationCodeRow
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string CodeChallenge { get; set; } = string.Empty;

    public string? Scope { get; set; }

    public DateTime ExpiresAtUtc { get; set; }
}
