// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs the web application and, under the Windows service control manager, lets the process exit only after the
/// service has been reported as stopped. Everywhere else it is plain app.Run().
/// </summary>
/// <param name="app">The fully initialised web application</param>

namespace Klacks.Api.Infrastructure.Hosting;

public static class KlacksWebApplicationRunner
{
    public static void RunKlacks(this WebApplication app)
    {
        var windowsService = OperatingSystem.IsWindows() ? app.Services.GetService<KlacksWindowsService>() : null;

        app.Run();

        if (OperatingSystem.IsWindows())
        {
            windowsService?.CompleteAfterHostExit();
        }
    }
}
