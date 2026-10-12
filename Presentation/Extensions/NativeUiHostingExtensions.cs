// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Serves the built Angular UI from Hosting:UiRoot on native installations, where no nginx and no UI container
/// exist. Mirrors the routing of deploy/onprem/nginx: server-side prefixes stay with the API (an unknown API route
/// answers 404), every other unknown GET route without a file extension gets index.html so deep links reach the
/// SPA router. index.html is never cached, so a browser picks up the new bundle names right after an update.
/// </summary>
/// <param name="uiRoot">Absolute directory containing index.html of the production UI build</param>

using Klacks.Api.Application.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;

namespace Klacks.Api.Presentation.Extensions;

public static class NativeUiHostingExtensions
{
    private const string IndexFileName = "index.html";
    private const string HtmlContentType = "text/html; charset=utf-8";
    private const string NoCache = "no-cache";
    private const char PathSeparator = '/';

    private static readonly string[] ServerOnlyPrefixes =
    [
        "/api",
        "/hubs",
        "/mcp",
        "/oauth",
        "/.well-known",
        "/health",
        "/internal",
    ];

    public static string? GetUiRoot(IConfiguration configuration)
    {
        var uiRoot = configuration[NativeHostingOptions.UiRootKey];
        return string.IsNullOrWhiteSpace(uiRoot) ? null : Path.GetFullPath(uiRoot);
    }

    public static IApplicationBuilder UseNativeUi(this IApplicationBuilder app, string? uiRoot)
    {
        if (uiRoot is null)
        {
            return app;
        }

        if (!File.Exists(Path.Combine(uiRoot, IndexFileName)))
        {
            throw new InvalidOperationException(
                $"{NativeHostingOptions.UiRootKey} points to '{uiRoot}', which contains no {IndexFileName}.");
        }

        var fileProvider = new PhysicalFileProvider(uiRoot);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = fileProvider,
            OnPrepareResponse = context =>
            {
                if (string.Equals(context.File.Name, IndexFileName, StringComparison.OrdinalIgnoreCase))
                {
                    context.Context.Response.Headers[HeaderNames.CacheControl] = NoCache;
                }
            },
        });
        return app;
    }

    public static IEndpointRouteBuilder MapNativeUiFallback(this IEndpointRouteBuilder endpoints, string? uiRoot)
    {
        if (uiRoot is null)
        {
            return endpoints;
        }

        var indexPath = Path.Combine(uiRoot, IndexFileName);
        endpoints.MapFallback(context => ServeIndexAsync(context, indexPath));
        return endpoints;
    }

    /// <summary>
    /// Answers an unmatched request: index.html for SPA navigations (GET/HEAD outside the server prefixes),
    /// 404 for everything else. The route pattern of MapFallback already excludes paths with a file extension.
    /// </summary>
    public static async Task ServeIndexAsync(HttpContext context, string indexPath)
    {
        var isNavigation = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method);
        if (!isNavigation || IsServerOnlyPath(context.Request.Path) || !File.Exists(indexPath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        context.Response.ContentType = HtmlContentType;
        context.Response.Headers[HeaderNames.CacheControl] = NoCache;
        if (HttpMethods.IsHead(context.Request.Method))
        {
            return;
        }

        await context.Response.SendFileAsync(indexPath);
    }

    public static bool IsServerOnlyPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return ServerOnlyPrefixes.Any(prefix =>
            value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && (value.Length == prefix.Length || value[prefix.Length] == PathSeparator));
    }
}
