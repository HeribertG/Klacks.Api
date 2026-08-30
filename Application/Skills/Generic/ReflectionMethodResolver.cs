// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves methods on repository interfaces including methods inherited from base interfaces.
/// <see cref="Type.GetMethod(string)"/> and <see cref="Type.GetMethod(string, Type[])"/> only search
/// methods declared on the exact type, so a method like <c>List()</c> that lives on
/// <c>IBaseRepository&lt;TEntity&gt;</c> is invisible on <c>ISchedulingRuleRepository</c> or
/// <c>IBranchRepository</c>. This walks the full interface hierarchy (breadth-first, self first)
/// so generic handler configs can reference inherited members.
/// </summary>

using System.Reflection;

namespace Klacks.Api.Application.Skills.Generic;

public static class ReflectionMethodResolver
{
    public static MethodInfo? FindOnInterface(Type interfaceType, string methodName, Type[] parameterTypes)
    {
        if (!interfaceType.IsInterface)
        {
            return null;
        }

        foreach (var candidate in EnumerateSelfAndBaseInterfaces(interfaceType))
        {
            var method = candidate.GetMethod(methodName, parameterTypes);
            if (method != null)
            {
                return method;
            }
        }

        // Fallback for parameterless invocations of methods whose parameters are all optional
        // (e.g. GetModelsAsync(bool onlyEnabled = false)): reflection does not treat an optional
        // parameter as parameterless, but the handler configs call such methods without arguments.
        if (parameterTypes.Length == 0)
        {
            foreach (var candidate in EnumerateSelfAndBaseInterfaces(interfaceType))
            {
                var allOptional = candidate.GetMethods()
                    .Where(m => string.Equals(m.Name, methodName, StringComparison.Ordinal)
                        && m.GetParameters().Length > 0
                        && m.GetParameters().All(p => p.HasDefaultValue))
                    .ToList();
                if (allOptional.Count == 1)
                {
                    return allOptional[0];
                }
            }
        }

        return null;
    }

    private static IEnumerable<Type> EnumerateSelfAndBaseInterfaces(Type interfaceType)
    {
        var visited = new HashSet<Type>();
        var queue = new Queue<Type>();
        queue.Enqueue(interfaceType);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            yield return current;

            foreach (var inherited in current.GetInterfaces())
            {
                if (!visited.Contains(inherited))
                {
                    queue.Enqueue(inherited);
                }
            }
        }
    }
}
