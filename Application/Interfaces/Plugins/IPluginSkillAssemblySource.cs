// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Reflection;

namespace Klacks.Api.Application.Interfaces.Plugins;

/// <summary>
/// Assemblies of the loaded plugins that carry skill implementations, scanned by SkillRegistryInitializer
/// next to the core assembly.
/// </summary>
public interface IPluginSkillAssemblySource
{
    IReadOnlyList<Assembly> GetSkillAssemblies();
}
