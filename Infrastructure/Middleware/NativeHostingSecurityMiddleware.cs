// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Restores on native installations what nginx provides in front of the Docker stack. Endpoints that were only reachable
/// inside the compose network (/internal, /_blazor) answer 404 unless the request comes from the machine itself, and
/// every response carries the security headers of deploy/onprem/nginx (nosniff, frame options, the same content
/// security policy, HSTS only over HTTPS). Registered only when Hosting:DataRoot is set.
/// </summary>
/// <param name="next">Next middleware in the pipeline</param>
/// <param name="configuration">Effective configuration; Hosting:MapTileCspSources replaces the tile hosts in img-src like
/// MAP_TILE_CSP_SOURCES does for nginx (an empty value allows no tile host)</param>

using System.Net;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Constants;
using Microsoft.Net.Http.Headers;

namespace Klacks.Api.Infrastructure.Middleware;

public class NativeHostingSecurityMiddleware
{
    private const char PathSeparator = '/';
    private const string NoSniff = "nosniff";
    private const string SameOrigin = "SAMEORIGIN";
    private const string ReferrerPolicyHeader = "Referrer-Policy";
    private const string ReferrerPolicy = "strict-origin-when-cross-origin";
    private const string HstsValue = "max-age=31536000; includeSubDomains";
    private const string ContentSecurityPolicyTemplate =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://unpkg.com; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://fonts.googleapis.com https://unpkg.com; " +
        "font-src 'self' data: https://fonts.gstatic.com https://cdnjs.cloudflare.com; " +
        "img-src 'self' data: blob: {0}; " +
        "media-src 'self' blob: data:; " +
        "connect-src 'self' wss: https: blob:; " +
        "worker-src 'self' blob:; " +
        "frame-ancestors 'self';";

    private static readonly string[] LoopbackOnlyPrefixes = ["/internal", "/_blazor"];

    private readonly RequestDelegate _next;
    private readonly string _contentSecurityPolicy;

    public NativeHostingSecurityMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var mapTileSources = configuration[NativeHostingOptions.MapTileCspSourcesKey] ?? GeoServiceDefaults.OsmTileCspSources;
        _contentSecurityPolicy = string.Format(ContentSecurityPolicyTemplate, mapTileSources);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsLoopbackOnlyPath(context.Request.Path) && !IsLocalRequest(context))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers[HeaderNames.XContentTypeOptions] = NoSniff;
            headers[HeaderNames.XFrameOptions] = SameOrigin;
            headers[ReferrerPolicyHeader] = ReferrerPolicy;
            headers[HeaderNames.ContentSecurityPolicy] = _contentSecurityPolicy;
            if (context.Request.IsHttps)
            {
                headers[HeaderNames.StrictTransportSecurity] = HstsValue;
            }

            return Task.CompletedTask;
        });

        await _next(context);
    }

    public static bool IsLoopbackOnlyPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return LoopbackOnlyPrefixes.Any(prefix =>
            value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && (value.Length == prefix.Length || value[prefix.Length] == PathSeparator));
    }

    public static bool IsLocalRequest(HttpContext context)
    {
        var remote = context.Connection.RemoteIpAddress;
        return remote is not null && IPAddress.IsLoopback(remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote);
    }
}
