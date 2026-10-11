// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure decision rule that turns the operator's GeoServices options into effective endpoints. A service is off
/// when the operator disabled it, when NoOutboundTraffic is set and no own URL is configured, when an own URL is
/// not an absolute http(s) URL, or when NoOutboundTraffic is set and the own URL points at a public OpenStreetMap
/// host. A misconfiguration never throws: the service is switched off instead, so a typo cannot crash-loop an
/// auto-updating installation.
/// </summary>
/// <param name="options">Endpoint options of one service as bound from configuration</param>
/// <param name="noOutboundTraffic">Main privacy switch: nothing may leave the installation</param>
/// <param name="publicDefaultUrl">Public default used when no own URL is configured</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Settings.GeoServices;

namespace Klacks.Api.Domain.Services.GeoServices;

public static class GeoServicesPolicyEvaluator
{
    private const string SubdomainPlaceholder = "{s}";
    private const string SubdomainProbe = "a";
    private const char SubdomainSeparator = ',';
    private const char UrlPathSeparator = '/';
    private const string HostLabelSeparator = ".";

    public static GeoServiceEndpoint Evaluate(
        GeoServiceEndpointOptions? options,
        bool noOutboundTraffic,
        string publicDefaultUrl)
    {
        var endpointOptions = options ?? new GeoServiceEndpointOptions();
        var (isEnabled, url, reason) = Decide(endpointOptions.Enabled, endpointOptions.BaseUrl, noOutboundTraffic, publicDefaultUrl);
        return new GeoServiceEndpoint(isEnabled, isEnabled ? url?.TrimEnd(UrlPathSeparator) : null, reason);
    }

    public static MapTileEndpoint EvaluateMapTiles(MapTileServiceOptions? options, bool noOutboundTraffic)
    {
        var tileOptions = options ?? new MapTileServiceOptions();
        var usesOwnTemplate = !string.IsNullOrWhiteSpace(tileOptions.UrlTemplate);
        var (isEnabled, template, reason) = Decide(
            tileOptions.Enabled, tileOptions.UrlTemplate, noOutboundTraffic, GeoServiceDefaults.OsmTileUrlTemplate);

        var attribution = string.IsNullOrWhiteSpace(tileOptions.Attribution)
            ? GeoServiceDefaults.OsmTileAttribution
            : tileOptions.Attribution.Trim();
        var maxZoom = tileOptions.MaxZoom is > 0 ? tileOptions.MaxZoom.Value : GeoServiceDefaults.OsmTileMaxZoom;
        var subdomains = usesOwnTemplate ? ParseSubdomains(tileOptions.Subdomains) : [];

        return new MapTileEndpoint(isEnabled, isEnabled ? template : null, subdomains, attribution, maxZoom, reason);
    }

    public static bool IsPublicHost(string host)
    {
        return GeoServiceDefaults.PublicHostDomains.Any(domain =>
            string.Equals(host, domain, StringComparison.OrdinalIgnoreCase)
            || host.EndsWith(HostLabelSeparator + domain, StringComparison.OrdinalIgnoreCase));
    }

    public static Uri? TryParseHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var probe = url.Trim().Replace(SubdomainPlaceholder, SubdomainProbe, StringComparison.Ordinal);
        if (!Uri.TryCreate(probe, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ? uri : null;
    }

    private static (bool IsEnabled, string? Url, GeoServiceDisabledReason Reason) Decide(
        bool enabled,
        string? configuredUrl,
        bool noOutboundTraffic,
        string publicDefaultUrl)
    {
        if (!enabled)
        {
            return (false, null, GeoServiceDisabledReason.DisabledByOperator);
        }

        if (string.IsNullOrWhiteSpace(configuredUrl))
        {
            return noOutboundTraffic
                ? (false, null, GeoServiceDisabledReason.NoOutboundTraffic)
                : (true, publicDefaultUrl, GeoServiceDisabledReason.None);
        }

        var uri = TryParseHttpUrl(configuredUrl);
        if (uri == null)
        {
            return (false, null, GeoServiceDisabledReason.InvalidConfiguration);
        }

        if (noOutboundTraffic && IsPublicHost(uri.Host))
        {
            return (false, null, GeoServiceDisabledReason.PublicHostBlocked);
        }

        return (true, configuredUrl.Trim(), GeoServiceDisabledReason.None);
    }

    private static IReadOnlyList<string> ParseSubdomains(string? subdomains)
    {
        if (string.IsNullOrWhiteSpace(subdomains))
        {
            return [];
        }

        return subdomains
            .Split(SubdomainSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }
}
