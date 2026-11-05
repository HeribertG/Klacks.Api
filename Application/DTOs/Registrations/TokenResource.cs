// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Session issued by login, refresh and the OAuth2 callback. The refresh token travels only in the HttpOnly cookie that
/// the controller writes from RefreshToken; it is never serialized into the response body.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Registrations;

public class TokenResource
{
    public bool Success { get; set; }

    public string Token { get; set; } = string.Empty;

    [JsonIgnore]
    public string RefreshToken { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Id { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public DateTime ExpTime { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsAuthorised { get; set; }

    public List<string> Permissions { get; set; } = new();

    public string Version { get; set; } = string.Empty;
}
