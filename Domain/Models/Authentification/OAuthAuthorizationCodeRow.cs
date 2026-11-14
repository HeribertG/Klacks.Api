// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persistent row for a one-time OAuth authorization code issued by the MCP authorization server.
/// Replaces the in-memory code store so the authorize call and the token exchange no longer have to
/// reach the same API instance: with the code in process memory, an exchange routed elsewhere is
/// rejected as invalid_grant even though the client did everything right, and every restart voids
/// all outstanding codes. Deliberately not a <c>BaseEntity</c>: the row is ephemeral and is
/// hard-deleted on redemption or expiry, so a redeemed code cannot be replayed from a leftover row.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Authentification;

public sealed class OAuthAuthorizationCodeRow
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string RedirectUri { get; set; } = string.Empty;

    public string CodeChallenge { get; set; } = string.Empty;

    public PersonalAccessTokenAccessMode AccessMode { get; set; } = PersonalAccessTokenAccessMode.Read;

    public DateTime ExpiresAtUtc { get; set; }
}
