using System.Collections.Concurrent;

namespace Klacks.Api.Infrastructure.Scripting;

public static class ImportCache
{
    private static readonly ConcurrentDictionary<(Type, string), Func<object, object?>> PropertyGetterCache = new();

    public static object? GetCachedPropertyValue(object target, string propertyName)
    {
        if (target == null) return null;

        var key = (target.GetType(), propertyName);

        var getter = PropertyGetterCache.GetOrAdd(key, k =>
        {
            var property = k.Item1.GetProperty(k.Item2);
            if (property == null) return _ => null;

            return obj => property.GetValue(obj);
        });

        return getter(target);
    }

    public static void ClearCache()
    {
        PropertyGetterCache.Clear();
    }
}
