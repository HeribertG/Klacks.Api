// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The caller's access token, carried to the skills so they can re-present it on a self-call against
/// the own REST API. It is a type rather than a plain string on purpose: <see cref="ToString"/> is
/// redacted, so the token cannot leak through the generated ToString of the records that hold it, and
/// the value has to be unwrapped explicitly wherever it is genuinely needed.
/// </summary>
/// <param name="Value">The raw bearer token as it arrived in the Authorization header</param>

using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record BearerToken([property: JsonIgnore] string Value)
{
    private const string Redacted = "***";

    public override string ToString() => Redacted;

    public static BearerToken? FromAuthorizationHeader(string? headerValue)
    {
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return null;
        }

        const string scheme = "Bearer ";
        var token = headerValue.StartsWith(scheme, StringComparison.OrdinalIgnoreCase)
            ? headerValue[scheme.Length..].Trim()
            : headerValue.Trim();

        return string.IsNullOrEmpty(token) ? null : new BearerToken(token);
    }
}
