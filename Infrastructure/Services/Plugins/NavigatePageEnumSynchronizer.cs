// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Owns the plugin navigation shape of the navigate_to skill: the route map persisted in its
/// HandlerConfig ({"routes":{"&lt;plugin&gt;":"&lt;route&gt;"}}) and the page keys derived from it into the
/// 'page' parameter enum. The plugin lifecycle and the skill seed loader both write that skill, so
/// parsing, serializing and the enum mutation live here once instead of drifting apart in two layers.
/// The page keys are a projection: the routes in HandlerConfig are the truth, the enum only makes them
/// reachable for the model, which is why it can be rebuilt from them at any time.
/// </summary>

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Plugins;

public static partial class NavigatePageEnumSynchronizer
{
    public const string PluginRoutePrefix = "/workplace/";

    private const string RoutesProperty = "routes";
    private const string ParameterNameProperty = "name";
    private const string PageParameterName = "page";
    private const string EnumValuesProperty = "enumValues";
    private const string EmptyJsonObject = "{}";

    private static readonly JsonSerializerOptions RouteWriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Decides whether a plugin manifest may contribute a route to the navigate_to skill. A manifest is
    /// installer-supplied data, so the route is checked against the one shape the frontend serves for a
    /// plugin page — a single lowercase segment under /workplace/ — instead of being stored as given.
    /// This keeps absolute URLs, traversal segments and query strings out of the route map the skill
    /// hands to the client as a navigation instruction.
    /// </summary>
    /// <param name="route">Route exactly as read from the plugin manifest</param>
    public static bool IsValidPluginRoute(string? route) =>
        !string.IsNullOrEmpty(route) && PluginRouteRegex().IsMatch(route);

    /// <summary>
    /// Reads the registered plugin routes out of a navigate_to handler config. A missing, empty or
    /// malformed config yields an empty map rather than an exception.
    /// </summary>
    /// <param name="handlerConfig">Raw handler config as stored on the skill</param>
    public static Dictionary<string, string> ParseRoutes(string? handlerConfig)
    {
        var routes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(handlerConfig) || handlerConfig == EmptyJsonObject)
        {
            return routes;
        }

        try
        {
            using var document = JsonDocument.Parse(handlerConfig);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty(RoutesProperty, out var routesElement)
                || routesElement.ValueKind != JsonValueKind.Object)
            {
                return routes;
            }

            foreach (var property in routesElement.EnumerateObject())
            {
                routes[property.Name] = property.Value.ValueKind == JsonValueKind.String
                    ? property.Value.GetString() ?? string.Empty
                    : string.Empty;
            }
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        return routes;
    }

    /// <summary>
    /// Writes the route map back in the shape PluginNavigationRouteCatalog reads.
    /// </summary>
    /// <param name="routes">Plugin name to route mapping to persist</param>
    public static string SerializeRoutes(Dictionary<string, string> routes) =>
        JsonSerializer.Serialize(new { routes }, RouteWriteOptions);

    /// <summary>
    /// Appends one page key to the 'page' enum when it is not present yet, leaving every other value
    /// and every other parameter property untouched.
    /// </summary>
    /// <param name="skill">The navigate_to skill whose ParametersJson is rewritten in place</param>
    /// <param name="pageKey">Page key to append, compared case-sensitively</param>
    public static void AddPageKey(AgentSkill skill, string pageKey) =>
        MutatePageKeys(skill, pageKeys => Append(pageKeys, pageKey));

    /// <summary>
    /// Drops one page key from the 'page' enum, leaving every other value in place.
    /// </summary>
    /// <param name="skill">The navigate_to skill whose ParametersJson is rewritten in place</param>
    /// <param name="pageKey">Page key to drop, compared case-sensitively</param>
    public static void RemovePageKey(AgentSkill skill, string pageKey) =>
        MutatePageKeys(skill, pageKeys => pageKeys.RemoveAll(key => string.Equals(key, pageKey, StringComparison.Ordinal)));

    /// <summary>
    /// Unions the page keys of the routes the skill still holds into its 'page' enum. A skill seed
    /// version bump rewrites ParametersJson from the seed file, which knows only the built-in pages —
    /// without this step the page of every installed plugin fell out of the enum while its route stayed
    /// in HandlerConfig, and the enum is validated hard before a navigation runs.
    /// </summary>
    /// <param name="skill">The navigate_to skill whose ParametersJson is rewritten in place</param>
    public static void UnionRegisteredRoutes(AgentSkill skill)
    {
        var routes = ParseRoutes(skill.HandlerConfig);
        if (routes.Count == 0)
        {
            return;
        }

        MutatePageKeys(skill, pageKeys =>
        {
            foreach (var pluginName in routes.Keys)
            {
                Append(pageKeys, pluginName);
            }
        });
    }

    private static void Append(List<string> pageKeys, string pageKey)
    {
        if (!string.IsNullOrEmpty(pageKey) && !pageKeys.Contains(pageKey, StringComparer.Ordinal))
        {
            pageKeys.Add(pageKey);
        }
    }

    private static void MutatePageKeys(AgentSkill skill, Action<List<string>> mutate)
    {
        if (string.IsNullOrWhiteSpace(skill.ParametersJson))
        {
            return;
        }

        try
        {
            if (JsonNode.Parse(skill.ParametersJson) is not JsonArray parameters || parameters.Count == 0)
            {
                return;
            }

            foreach (var parameter in parameters)
            {
                if (parameter is not JsonObject parameterObject
                    || parameterObject[ParameterNameProperty]?.GetValue<string>() != PageParameterName)
                {
                    continue;
                }

                var pageKeys = ReadPageKeys(parameterObject);
                mutate(pageKeys);

                parameterObject[EnumValuesProperty] = new JsonArray(
                    pageKeys.Select(key => (JsonNode?)JsonValue.Create(key)).ToArray());
            }

            skill.ParametersJson = parameters.ToJsonString();
        }
        catch (JsonException)
        {
        }
    }

    [GeneratedRegex("^" + PluginRoutePrefix + "[a-z0-9-]+$")]
    private static partial Regex PluginRouteRegex();

    private static List<string> ReadPageKeys(JsonObject parameterObject)
    {
        if (parameterObject[EnumValuesProperty] is not JsonArray enumValues)
        {
            return new List<string>();
        }

        return enumValues
            .Select(value => value?.GetValue<string>())
            .Where(value => !string.IsNullOrEmpty(value))
            .Select(value => value!)
            .ToList();
    }
}
