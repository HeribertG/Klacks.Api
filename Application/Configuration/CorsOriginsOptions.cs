// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The origins of the CORS policy, bound from the "Cors" section: Host, Home and the comma-separated Additional list.
/// </summary>
/// <param name="Host">Public origin of the installation</param>
/// <param name="Home">Origin the Ui is served from</param>
/// <param name="Additional">Further origins, comma-separated</param>
namespace Klacks.Api.Application.Configuration;

public class CorsOriginsOptions
{
    public const string SectionName = "Cors";

    public const string PolicyName = "CorsPolicy";

    private const char AdditionalSeparator = ',';

    public string? Host { get; set; }

    public string? Home { get; set; }

    public string? Additional { get; set; }

    public string[] AllowedOrigins()
    {
        var additional = string.IsNullOrEmpty(Additional)
            ? []
            : Additional.Split(AdditionalSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new[] { Host, Home }
            .Concat(additional)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin!)
            .ToArray();
    }
}
