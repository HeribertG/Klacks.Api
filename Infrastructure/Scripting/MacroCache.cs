// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using Klacks.Api.Infrastructure.Interfaces;

namespace Klacks.Api.Infrastructure.Scripting;

public class MacroCache : IMacroCache
{
    private readonly ConcurrentDictionary<Guid, CompiledScript> _cache = new();

    public CompiledScript GetOrCompile(Guid macroId, string script)
    {
        return _cache.GetOrAdd(macroId, _ => CompiledScript.Compile(script));
    }

    public void Invalidate(Guid macroId)
    {
        _cache.TryRemove(macroId, out _);
    }

    public void InvalidateAll()
    {
        _cache.Clear();
    }

    public bool Contains(Guid macroId)
    {
        return _cache.ContainsKey(macroId);
    }
}
