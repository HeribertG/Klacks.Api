// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Static registry for plugin EF model configurations.
/// Plugins register their ModelBuilder actions at startup, applied during OnModelCreating.
/// </summary>

using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Plugins;

public static class PluginModelRegistry
{
    private static readonly List<Action<ModelBuilder>> Configurers = [];

    public static void Register(Action<ModelBuilder> configurer)
    {
        Configurers.Add(configurer);
    }

    public static void Apply(ModelBuilder builder)
    {
        foreach (var configurer in Configurers)
        {
            configurer(builder);
        }
    }

    public static void Clear()
    {
        Configurers.Clear();
    }
}
