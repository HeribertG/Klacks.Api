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

namespace Klacks.Api.Application.Configuration;

public class NativeHostingOptions
{
    public const string SectionName = "Hosting";

    public const string DataRootKey = SectionName + ":" + nameof(DataRoot);

    public const string UiRootKey = SectionName + ":" + nameof(UiRoot);

    public const string DataRootEnvironmentVariable = SectionName + "__" + nameof(DataRoot);

    public const string WindowsServiceName = "Klacks";

    public string DataRoot { get; set; } = string.Empty;

    public string UiRoot { get; set; } = string.Empty;
}
