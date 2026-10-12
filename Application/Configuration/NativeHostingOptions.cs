// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Settings for running Klacks natively (Windows service, launchd) instead of in a container. Every value is
/// empty by default, which keeps the container behaviour byte-identical.
/// </summary>
/// <param name="DataRoot">Absolute directory for everything the process writes. When set, relative writable paths
/// (images, documents, ERP import, payroll exports, model cache, demo seed file, file key ring) resolve below it
/// instead of below the read-only, versioned program directory.</param>
/// <param name="UiRoot">Absolute directory of the built Angular UI. When set, the API serves it and answers unknown
/// non-API routes with its index.html, replacing the nginx and UI containers.</param>
/// <remarks>A native installation keeps its secrets (connection string, JWT secret, DataProtection certificate) in
/// &lt;DataRoot&gt;/config/klacks.native.json, readable by SYSTEM and administrators only, instead of service environment
/// variables in the registry, which every local user can read.</remarks>

namespace Klacks.Api.Application.Configuration;

public class NativeHostingOptions
{
    public const string SectionName = "Hosting";

    public const string DataRootKey = SectionName + ":" + nameof(DataRoot);

    public const string UiRootKey = SectionName + ":" + nameof(UiRoot);

    public const string DataRootEnvironmentVariable = SectionName + "__" + nameof(DataRoot);

    public const string MapTileCspSourcesKey = SectionName + ":MapTileCspSources";

    public const string WindowsServiceName = "Klacks";

    public const string ConfigDirectoryName = "config";

    public const string ConfigFileName = "klacks.native.json";

    public string DataRoot { get; set; } = string.Empty;

    public string UiRoot { get; set; } = string.Empty;
}
