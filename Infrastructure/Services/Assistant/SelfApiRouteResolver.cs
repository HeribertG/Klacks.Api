// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps a resource type to the route of the controller that serves it, by reflecting over the generic
/// CRUD controllers once at startup. Skills calling the own API therefore never carry a hand-typed route
/// that can drift from the controller: renaming a controller moves the route here too, and a resource
/// without a controller fails loudly at the call site instead of producing a 404 the model has to
/// interpret. Routes are read from the [Route] attribute — inherited from BaseController for most
/// controllers — with the [controller] token expanded the same way ASP.NET does.
/// </summary>

using System.Reflection;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Presentation.Controllers.UserBackend;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public sealed class SelfApiRouteResolver : ISelfApiRouteResolver
{
    private const string ControllerSuffix = "Controller";
    private const string ControllerToken = "[controller]";

    private readonly IReadOnlyDictionary<Type, IReadOnlyList<string>> _routesByResource;

    public SelfApiRouteResolver()
    {
        _routesByResource = BuildMap(typeof(InputBaseController<>).Assembly);
    }

    public string Resolve(Type resourceType)
    {
        if (TryResolve(resourceType, out var route))
        {
            return route;
        }

        if (_routesByResource.TryGetValue(resourceType, out var candidates))
        {
            throw new InvalidOperationException(
                $"'{resourceType.Name}' is served by more than one controller ({string.Join(", ", candidates)}), " +
                "so the route cannot be derived from the type alone. The calling skill has to name the route " +
                "it means explicitly.");
        }

        throw new InvalidOperationException(
            $"No generic CRUD controller serves '{resourceType.Name}'. A skill cannot mutate it over the " +
            "REST API until one exists — see the rights-unification plan, phase 2.1.");
    }

    /// <summary>
    /// Resolves only when exactly one controller serves the type. Several controllers can be typed on the
    /// same resource — GroupResource is served both by the groups endpoint and by the group-visibility
    /// one — and silently picking either would send a write to the wrong place.
    /// </summary>
    /// <param name="resourceType">The resource DTO</param>
    /// <param name="route">The single route serving it</param>
    public bool TryResolve(Type resourceType, out string route)
    {
        if (_routesByResource.TryGetValue(resourceType, out var routes) && routes.Count == 1)
        {
            route = routes[0];
            return true;
        }

        route = string.Empty;
        return false;
    }

    internal static Dictionary<Type, IReadOnlyList<string>> BuildMap(Assembly assembly)
    {
        var map = new Dictionary<Type, List<string>>();

        foreach (var controller in assembly.GetTypes().Where(IsGenericCrudController))
        {
            var resourceType = ResolveResourceType(controller);
            if (resourceType is null)
            {
                continue;
            }

            var route = ResolveRoute(controller);
            if (route is null)
            {
                continue;
            }

            if (!map.TryGetValue(resourceType, out var routes))
            {
                routes = [];
                map[resourceType] = routes;
            }

            if (!routes.Contains(route))
            {
                routes.Add(route);
            }
        }

        return map.ToDictionary(entry => entry.Key, entry => (IReadOnlyList<string>)entry.Value);
    }

    private static bool IsGenericCrudController(Type type) =>
        type is { IsClass: true, IsAbstract: false } && ResolveResourceType(type) is not null;

    private static Type? ResolveResourceType(Type type)
    {
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(InputBaseController<>))
            {
                return current.GetGenericArguments()[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Reads the route the same way routing does: the nearest [Route] up the hierarchy, with
    /// [controller] replaced by the class name minus its suffix.
    /// </summary>
    private static string? ResolveRoute(Type controller)
    {
        for (var current = controller; current is not null; current = current.BaseType)
        {
            var template = current.GetCustomAttribute<RouteAttribute>(inherit: false)?.Template;
            if (string.IsNullOrWhiteSpace(template))
            {
                continue;
            }

            var name = controller.Name.EndsWith(ControllerSuffix, StringComparison.Ordinal)
                ? controller.Name[..^ControllerSuffix.Length]
                : controller.Name;

            return template.Replace(ControllerToken, name, StringComparison.OrdinalIgnoreCase).Trim('/');
        }

        return null;
    }
}
