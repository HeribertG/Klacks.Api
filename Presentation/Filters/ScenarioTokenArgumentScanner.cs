// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Finds every scenario token a request carries in its bound action arguments: a simple argument named
/// "analyseToken" (query, route or body) and every Guid/Guid? property named "AnalyseToken" inside bound Klacks DTOs,
/// including nested DTOs and collection elements (e.g. BulkAddWorksRequest.Works[i].AnalyseToken). Which properties of
/// a type are worth walking is decided once per type from its static property graph and cached, so DTOs that cannot
/// reach a token are never walked.
/// </summary>
/// <param name="arguments">The bound action arguments (name to value)</param>
/// <param name="type">A bound argument type whose token reachability or depth is asked for</param>

using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Klacks.Api.Presentation.Filters;

public static class ScenarioTokenArgumentScanner
{
    public const string TokenName = "AnalyseToken";

    /// <summary>
    /// Deepest level the value walk descends to. The root argument is level 0, each nested DTO or collection adds one
    /// (BulkAddWorksRequest.Works[i] is level 2; the deepest path today is ContainerTemplateResource lists at level 7). ScenarioTokenFilterCoverageGuardTests proves every token reachable
    /// from a controller action lies within it.
    /// </summary>
    public const int MaxDepth = 8;

    private const string ScannableNamespacePrefix = "Klacks.";

    private static readonly ConcurrentDictionary<Type, TypePlan> Plans = new();

    public static IReadOnlySet<Guid> Collect(IDictionary<string, object?> arguments)
    {
        var tokens = new HashSet<Guid>();
        foreach (var (name, value) in arguments)
        {
            if (value is null)
            {
                continue;
            }

            if (string.Equals(name, TokenName, StringComparison.OrdinalIgnoreCase))
            {
                AddToken(value, tokens);
                continue;
            }

            Walk(value, tokens, 0);
        }

        return tokens;
    }

    public static bool CanCarryToken(Type type) => GetPlan(type).CanCarryToken;

    /// <summary>
    /// Deepest level at which a token property is reachable from the type, following the same rules as the value walk;
    /// cycles are cut. -1 when the type cannot carry a token.
    /// </summary>
    public static int MaxTokenDepth(Type type) => MaxTokenDepth(type, 0, new HashSet<Type>());

    private static int MaxTokenDepth(Type type, int depth, HashSet<Type> path)
    {
        var plan = GetPlan(type);
        if (!plan.CanCarryToken || !path.Add(type))
        {
            return -1;
        }

        var deepest = plan.TokenProperties.Length > 0 ? depth : -1;
        if (plan.ElementType is { } element)
        {
            deepest = Math.Max(deepest, MaxTokenDepth(element, depth + 1, path));
        }

        foreach (var child in plan.ChildProperties)
        {
            deepest = Math.Max(deepest, MaxTokenDepth(child.PropertyType, depth + 1, path));
        }

        path.Remove(type);
        return deepest;
    }

    private static void Walk(object value, HashSet<Guid> tokens, int depth)
    {
        if (depth > MaxDepth)
        {
            return;
        }

        var plan = GetPlan(value.GetType());
        if (!plan.CanCarryToken)
        {
            return;
        }

        if (plan.ElementType is not null && value is IEnumerable items)
        {
            foreach (var item in items)
            {
                if (item is not null)
                {
                    Walk(item, tokens, depth + 1);
                }
            }

            return;
        }

        foreach (var tokenProperty in plan.TokenProperties)
        {
            AddToken(tokenProperty.GetValue(value), tokens);
        }

        foreach (var child in plan.ChildProperties)
        {
            if (child.GetValue(value) is { } childValue)
            {
                Walk(childValue, tokens, depth + 1);
            }
        }
    }

    private static void AddToken(object? value, HashSet<Guid> tokens)
    {
        if (value is Guid token && token != Guid.Empty)
        {
            tokens.Add(token);
        }
    }

    private static TypePlan GetPlan(Type type) => Plans.GetOrAdd(type, BuildPlan);

    private static TypePlan BuildPlan(Type type)
    {
        if (!ReachesToken(type))
        {
            return TypePlan.None;
        }

        if (ElementTypeOf(type) is { } element)
        {
            return new TypePlan(true, [], [], element);
        }

        var properties = ReadableProperties(type);
        var tokenProperties = properties.Where(IsTokenProperty).ToArray();
        var childProperties = properties
            .Where(p => !IsTokenProperty(p) && IsWalkable(p.PropertyType) && ReachesToken(p.PropertyType))
            .ToArray();

        return new TypePlan(true, tokenProperties, childProperties, null);
    }

    private static bool ReachesToken(Type root)
    {
        var seen = new HashSet<Type>();
        var queue = new Queue<Type>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            if (!seen.Add(type) || !IsWalkable(type))
            {
                continue;
            }

            if (ElementTypeOf(type) is { } element)
            {
                queue.Enqueue(element);
                continue;
            }

            foreach (var property in ReadableProperties(type))
            {
                if (IsTokenProperty(property))
                {
                    return true;
                }

                queue.Enqueue(property.PropertyType);
            }
        }

        return false;
    }

    private static bool IsTokenProperty(PropertyInfo property) =>
        property.Name == TokenName && (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?));

    private static bool IsWalkable(Type type) =>
        ElementTypeOf(type) is not null || IsScannableType(type);

    private static bool IsScannableType(Type type) =>
        !type.IsPrimitive && !type.IsEnum && type.Namespace?.StartsWith(ScannableNamespacePrefix, StringComparison.Ordinal) == true;

    private static Type? ElementTypeOf(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }

        if (type.IsArray)
        {
            return type.GetElementType();
        }

        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        return enumerable?.GetGenericArguments()[0];
    }

    private static PropertyInfo[] ReadableProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToArray();

    private sealed record TypePlan(
        bool CanCarryToken, PropertyInfo[] TokenProperties, PropertyInfo[] ChildProperties, Type? ElementType)
    {
        public static readonly TypePlan None = new(false, [], [], null);
    }
}
