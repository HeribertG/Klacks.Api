// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Where the API reaches itself when a skill mutates state through the own REST endpoints instead of
/// writing to the database directly. Deliberately its own section rather than reusing Mcp:PublicBaseUrl
/// or PasswordReset:BaseUrl — those are outward-facing URLs a deployment may point at a proxy, while
/// this one must stay a loopback address the process can actually connect to.
/// </summary>

namespace Klacks.Api.Application.Configuration;

public class SelfApiOptions
{
    public const string SectionName = "SelfApi";

    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Accepts the development certificate for the self URL only. Never enable this in production —
    /// the loopback call would then trust any certificate presented on that address.
    /// </summary>
    public bool AcceptAnyServerCertificate { get; set; }

    public int TimeoutSeconds { get; set; } = 30;
}
