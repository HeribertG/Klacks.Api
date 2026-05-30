// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Reflection;
using System.Runtime.InteropServices;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.ML.OnnxRuntime;
using Tokenizers.DotNet;

namespace Klacks.Api.Infrastructure.StartupChecks;

/// <summary>
/// Deep health check that verifies platform-specific native libraries and the pgvector database extension.
/// Caches native library results at startup since they don't change during process lifetime.
/// </summary>
/// <param name="scopeFactory">Factory used to create a DI scope for database access.</param>
public sealed class PlatformDependencyHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    private const string StatusHealthy = "Healthy";
    private const string StatusUnhealthy = "Unhealthy";
    private const string LdapLibraryPattern = "lib{0}.so.2";
    private const string OnnxNativeLibrary = "onnxruntime";
    private const string TokenizersNativeLibrary = "hf_tokenizers";
    private const string PgvectorCheckSql = "SELECT name FROM pg_available_extensions WHERE name = 'vector'";

    private static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    private static readonly Dictionary<string, bool> NativeLibResults = CheckNativeLibraries();

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>();

        foreach (var (name, loaded) in NativeLibResults)
        {
            data[name] = loaded ? StatusHealthy : StatusUnhealthy;
        }

        await CheckPgvectorAsync(data, cancellationToken);

        var allHealthy = data.Values.All(v => v is string s && s == StatusHealthy);
        var status = allHealthy ? HealthStatus.Healthy : HealthStatus.Unhealthy;

        return new HealthCheckResult(status, data: data);
    }

    private static Dictionary<string, bool> CheckNativeLibraries()
    {
        var results = new Dictionary<string, bool>();

        // onnxruntime and hf_tokenizers ship as NuGet RID-native assets under runtimes/<rid>/native and
        // are resolved via each wrapper assembly's deps.json, not the OS loader search path. A bare
        // NativeLibrary.TryLoad("onnxruntime") therefore always fails (the file is libonnxruntime.so off
        // the default path, and the tokenizer file is libhf_tokenizers.so, never libtokenizers.so). The
        // assembly-context overload mirrors exactly how the application itself P/Invokes these libraries.
        results["onnx_runtime"] = TryLoadFromAssembly(OnnxNativeLibrary, typeof(InferenceSession).Assembly);
        results["tokenizers"] = TryLoadFromAssembly(TokenizersNativeLibrary, typeof(Tokenizer).Assembly);
        results["ldap"] = IsLinux
            ? TryLoadAndFree(string.Format(LdapLibraryPattern, "ldap"))
            : true;

        return results;
    }

    private static bool TryLoadFromAssembly(string libraryName, Assembly assembly)
    {
        if (!NativeLibrary.TryLoad(libraryName, assembly, DllImportSearchPath.AssemblyDirectory, out var handle))
            return false;

        NativeLibrary.Free(handle);
        return true;
    }

    private static bool TryLoadAndFree(string libraryName)
    {
        if (!NativeLibrary.TryLoad(libraryName, out var handle))
            return false;

        NativeLibrary.Free(handle);
        return true;
    }

    private async Task CheckPgvectorAsync(
        Dictionary<string, object> data,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
            var result = await db.Database
                .SqlQueryRaw<string>(PgvectorCheckSql)
                .ToListAsync(cancellationToken);
            data["pgvector"] = result.Count > 0 ? StatusHealthy : StatusUnhealthy;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            data["pgvector"] = StatusUnhealthy;
        }
    }
}
