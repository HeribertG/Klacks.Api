// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Verifies that required native libraries are available at startup.
/// Fails fast with a clear error message instead of crashing at runtime.
/// </summary>
using System.Runtime.InteropServices;

namespace Klacks.Api.Infrastructure.StartupChecks;

public static class NativeLibraryCheck
{
    private static readonly string[] RequiredLibraries = ["ldap"];

    public static void Verify()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }

        var missing = new List<string>();

        foreach (var lib in RequiredLibraries)
        {
            if (!NativeLibrary.TryLoad($"lib{lib}.so.2", out _))
            {
                missing.Add($"lib{lib}.so.2");
            }
        }

        if (missing.Count > 0)
        {
            var message = $"FATAL: Required native libraries missing: {string.Join(", ", missing)}. " +
                          "LDAP authentication will not work. " +
                          "Install via: apt-get install -y libldap2";
            Console.Error.WriteLine(message);
            throw new InvalidOperationException(message);
        }

        Console.WriteLine("Native library check passed: {0}", string.Join(", ", RequiredLibraries.Select(l => $"lib{l}.so.2")));
    }
}
