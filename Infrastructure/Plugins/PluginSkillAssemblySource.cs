// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the skill assemblies of the plugin registrars collected while the services were registered.
/// Stateless: every call reflects the registrars known at that moment.
/// </summary>

using System.Reflection;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Infrastructure.Extensions;

namespace Klacks.Api.Infrastructure.Plugins;

public sealed class PluginSkillAssemblySource : IPluginSkillAssemblySource
{
    public IReadOnlyList<Assembly> GetSkillAssemblies() =>
        ServiceCollectionExtensions.GetPluginRegistrars()
            .SelectMany(registrar => registrar.GetSkillAssemblies())
            .ToList();
}
