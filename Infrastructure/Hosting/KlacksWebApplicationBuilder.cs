// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates the web application builder for every hosting mode (container, Windows service, launchd, development).
/// Native installations start with an unrelated working directory (System32 for a Windows service, / under launchd),
/// so their content root is pinned to the program directory and all writable paths move below Hosting:DataRoot.
/// Their settings and secrets come from &lt;DataRoot&gt;/config/klacks.native.json, which overrides every other source.
/// Without a Windows service and without the Hosting__DataRoot environment variable the builder is identical to
/// WebApplication.CreateBuilder(args).
/// </summary>
/// <param name="args">Command line arguments of the process</param>

using System.Runtime.Versioning;
using Klacks.Api.Application.Configuration;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Klacks.Api.Infrastructure.Hosting;

public static class KlacksWebApplicationBuilder
{
    public static WebApplicationBuilder Create(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ContentRootPath = IsNativeInstallation() ? AppContext.BaseDirectory : null,
        });

        AddNativeConfigurationFile(builder.Configuration);
        builder.Configuration.AddInMemoryCollection(DataRootPaths.BuildConfigurationOverrides(builder.Configuration));

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
        {
            AddWindowsServiceHosting(builder);
        }

        return builder;
    }

    [SupportedOSPlatform("windows")]
    private static void AddWindowsServiceHosting(WebApplicationBuilder builder)
    {
        var service = new KlacksWindowsService();
        service.StartDispatcher();
        builder.Services.AddSingleton(service);
        builder.Services.AddHostedService<KlacksWindowsServiceStatusBridge>();
        builder.Logging.AddEventLog(settings => settings.SourceName = builder.Environment.ApplicationName);
    }

    private static void AddNativeConfigurationFile(ConfigurationManager configuration)
    {
        var dataRoot = DataRootPaths.GetDataRoot(configuration);
        if (dataRoot is null)
        {
            return;
        }

        var file = Path.Combine(dataRoot, NativeHostingOptions.ConfigDirectoryName, NativeHostingOptions.ConfigFileName);
        configuration.AddJsonFile(file, optional: true, reloadOnChange: false);
    }

    private static bool IsNativeInstallation()
    {
        return WindowsServiceHelpers.IsWindowsService()
            || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(NativeHostingOptions.DataRootEnvironmentVariable));
    }
}
