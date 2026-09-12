// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the plugin navigation routes that FeaturePluginService.RegisterPluginNavigationAsync writes
/// into the navigate_to skill's HandlerConfig ({"routes":{"&lt;plugin&gt;":"&lt;route&gt;"}}). The skill
/// registry is the read path on purpose: it serves the routes without a database round-trip per
/// navigation, and it is as current as the last catalogue rebuild — so the routes here are the stored
/// ones for as long as install, uninstall, enable and disable rebuild the catalogue successfully. Those
/// rebuilds are best-effort (their failures are logged and swallowed), so a failed one leaves this
/// catalogue on the previous routes until the next rebuild or application start. A malformed or absent
/// config yields no routes rather than an exception — navigation must not break because a plugin wrote
/// garbage.
/// </summary>
/// <param name="skillRegistry">In-memory skill catalogue holding the navigate_to descriptor.</param>
namespace Klacks.Api.Application.Klacksy;

using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;

public sealed class PluginNavigationRouteCatalog : IPluginNavigationRouteCatalog
{
    private const string NavigateToSkillName = "navigate_to";
    private const string RoutesProperty = "routes";

    private readonly ISkillRegistry _skillRegistry;

    public PluginNavigationRouteCatalog(ISkillRegistry skillRegistry)
    {
        _skillRegistry = skillRegistry;
    }

    public string? GetRoute(string pageKey)
    {
        if (string.IsNullOrWhiteSpace(pageKey))
        {
            return null;
        }

        var handlerConfig = _skillRegistry.GetSkillByName(NavigateToSkillName)?.HandlerConfig;
        if (string.IsNullOrWhiteSpace(handlerConfig))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(handlerConfig);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(RoutesProperty, out var routes)
                || routes.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var route in routes.EnumerateObject())
            {
                if (route.NameEquals(pageKey) || string.Equals(route.Name, pageKey, StringComparison.OrdinalIgnoreCase))
                {
                    var value = route.Value.ValueKind == JsonValueKind.String ? route.Value.GetString() : null;
                    return string.IsNullOrWhiteSpace(value) ? null : value;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}
