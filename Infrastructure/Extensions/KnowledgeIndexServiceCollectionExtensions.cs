// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Knowledge-index registrations and the three configuration resolvers only they use, split out of
/// ServiceCollectionExtensions so that file stays under its size-guard ceiling. Names, visibility and
/// order are unchanged, so the tests that compose AddKnowledgeIndexServices or pin
/// ResolveOnnxAllowIntraOpSpinning on their own keep working.
/// </summary>
using System.Runtime.InteropServices;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Application.Skills;
using Klacks.Api.Infrastructure.Scripting;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Staffs;
using Klacks.Api.Infrastructure.Repositories.Assistant;
using Klacks.Api.Domain.Services.Absences;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Infrastructure.Services.CalendarSelections;
using Klacks.Api.Domain.Services.Clients;
using Klacks.Api.Infrastructure.Services.Clients;
using Klacks.Api.Domain.Services.ContainerTemplates;
using Klacks.Api.Domain.Services.Groups;
using Klacks.Api.Domain.Services.Holidays;
using Klacks.Api.Domain.Services.Settings;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Application.Common;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Services.ScheduleEntries;
using Klacks.Api.Infrastructure.Services.AnalyseScenarios;
using Klacks.Api.Infrastructure.Services.PeriodHours;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Services.Assistant;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.RouteOptimization;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Services.Macros;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Services.Schedules;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Email;
using Klacks.Api.Infrastructure.FileHandling;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories;
using Klacks.Api.Infrastructure.Repositories.Associations;
using Klacks.Api.Infrastructure.Repositories.Email;
using Klacks.Api.Infrastructure.Repositories.Authentification;
using Klacks.Api.Infrastructure.Repositories.CalendarSelections;
using Klacks.Api.Infrastructure.Repositories.Imports;
using Klacks.Api.Infrastructure.Repositories.Reports;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Klacks.Api.Infrastructure.Repositories.Scheduling;
using Klacks.Api.Infrastructure.Repositories.Settings;
using Klacks.Api.Infrastructure.Repositories.Exports;
using Klacks.Api.Infrastructure.Repositories.Staffs;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Infrastructure.Services.Groups;
using Klacks.Api.Infrastructure.Services.Settings;
using Klacks.Api.Infrastructure.Services.Shifts;
using Klacks.Api.Application.Services.Authentication;
using Klacks.Api.Application.Services.Clients;
using Klacks.Api.Application.Services.Identity;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Translation;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Infrastructure.Services.Associations;
using Klacks.Api.Infrastructure.Services.Plugins;
using Klacks.Api.Infrastructure.Services.ClientAvailabilitySchedule;
using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Skills.Generated;
using Klacks.Api.Application.Skills.Generic;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Application.Services;
using Klacks.Api.KnowledgeIndex.Infrastructure.Api;
using Klacks.Api.KnowledgeIndex.Infrastructure.Onnx;
using Klacks.Api.KnowledgeIndex.Infrastructure.Persistence;
using Klacks.Api.KnowledgeIndex.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Extensions;

internal static class KnowledgeIndexServiceCollectionExtensions
{
    // internal so a unit test can assert the registration shape - one provider instance behind three
    // service types - against a bare ServiceCollection, without booting a host that would immediately
    // warm the sessions and load 674 MB of models.
    internal static void AddKnowledgeIndexServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient(KnowledgeIndexConstants.HttpClientName);

        // Registered here rather than relied upon from AddLLMCoreServices: the idle unload sweep needs
        // it, and a registration method that only works when another one ran first is a trap for both
        // the next caller and every test that composes this method on its own. TryAdd, so the existing
        // registration in the LLM block stays the single instance wherever both run.
        services.TryAddSingleton(TimeProvider.System);

        // Harmless in the non-ONNX branch too: it holds no state and reports false on every platform
        // whose allocator has no malloc_trim, which is every platform that is not Linux/glibc.
        services.AddSingleton<IProcessHeapTrimmer, GlibcHeapTrimmer>();

        var modelsRoot = ResolveModelsRoot(configuration);
        var onnxSupported = IsOnnxRuntimeSupported(configuration);

        services.AddSingleton<ModelLoader>(sp =>
            new ModelLoader(sp.GetRequiredService<IHttpClientFactory>().CreateClient(KnowledgeIndexConstants.HttpClientName)));

        if (onnxSupported)
        {
            // One instance, three service types. Registering the interfaces with their own factory
            // lambda would build a SECOND session per interface - 608 MB of model weights that nothing
            // ever calls and no idle sweep would ever recognise as unused.
            services.AddSingleton<OnnxEmbeddingProvider>(sp => new OnnxEmbeddingProvider(
                sp.GetRequiredService<ModelLoader>(),
                Path.Combine(modelsRoot, KnowledgeIndexConstants.EmbeddingModelName)));
            services.AddSingleton<IEmbeddingProvider>(sp => sp.GetRequiredService<OnnxEmbeddingProvider>());
            services.AddSingleton<IUnloadableInferenceSession>(sp => sp.GetRequiredService<OnnxEmbeddingProvider>());

            var allowIntraOpSpinning = ResolveOnnxAllowIntraOpSpinning(configuration);
            services.AddSingleton<OnnxRerankerProvider>(sp => new OnnxRerankerProvider(
                sp.GetRequiredService<ModelLoader>(),
                Path.Combine(modelsRoot, KnowledgeIndexConstants.RerankerModelName),
                profile: OnnxRerankerRuntimeProfile.ForIntraOpSpinning(allowIntraOpSpinning)));
            services.AddSingleton<IRerankerProvider>(sp => sp.GetRequiredService<OnnxRerankerProvider>());
            services.AddSingleton<IUnloadableInferenceSession>(sp => sp.GetRequiredService<OnnxRerankerProvider>());
        }
        else
        {
            // Local-development fallback for hosts where ONNX cannot run (Windows ARM64): reuses the
            // "openai" LLM provider's API key already configured for chat instead of a separate secret.
            // A real (if weaker than the ONNX cross-encoder) similarity signal. Production (Hetzner,
            // x64) always takes the onnxSupported branch above and never reaches this; if no "openai"
            // provider key is configured either, this fails loud per-call (caught by the semantic
            // recipe match / retrieval call sites) instead of the old silent NullEmbeddingProvider no-op.
            // Was GeminiEmbeddingProvider until 2026-07-25: gemini-embedding-001 started returning
            // HTTP 429 "prepayment credits are depleted" on every call, which silently reduced each
            // chat turn to the always-on skills. text-embedding-3-small honours the "dimensions"
            // parameter, so it needs no truncation workaround. GeminiEmbeddingProvider is kept as a
            // drop-in alternative — swapping the type here is the whole switch.
            services.AddScoped<ILlmProviderCredentialReader, LlmProviderCredentialReader>();
            services.AddScoped<IEmbeddingProvider>(sp => new OpenAiEmbeddingProvider(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(KnowledgeIndexConstants.HttpClientName),
                sp.GetRequiredService<ILlmProviderCredentialReader>()));
            services.AddScoped<IRerankerProvider>(sp =>
                new EmbeddingSimilarityRerankerProvider(sp.GetRequiredService<IEmbeddingProvider>()));
        }

        services.AddScoped<IKnowledgeIndexRepository>(sp =>
        {
            var context = sp.GetRequiredService<DataBaseContext>();
            var conn = (NpgsqlConnection)context.Database.GetDbConnection();
            if (conn.State == System.Data.ConnectionState.Closed)
                conn.Open();
            return new KnowledgeIndexRepository(conn);
        });

        var snapshotEnabled = configuration.GetValue<bool>(KnowledgeIndexConstants.SnapshotEnabledConfigKey, true);
        services.AddSingleton<IKnowledgeEmbeddingSnapshotReader>(sp => new FileKnowledgeEmbeddingSnapshotReader(
            ResolveSnapshotFile(configuration, sp.GetRequiredService<IHostEnvironment>()),
            snapshotEnabled,
            sp.GetRequiredService<ILogger<FileKnowledgeEmbeddingSnapshotReader>>()));

        services.AddScoped<IKnowledgeEmbeddingSnapshotExporter, KnowledgeEmbeddingSnapshotExporter>();

        services.AddScoped<IKnowledgeIndexSynchronizer, KnowledgeIndexSynchronizer>();

        // Singleton, because the single-flight gate and the coalescing flag only mean something
        // process-wide. It resolves the scoped synchronizer from a fresh scope per run.
        services.AddSingleton<IKnowledgeIndexSyncScheduler, KnowledgeIndexSyncScheduler>();
        services.AddScoped<IKnowledgeRetrievalService, KnowledgeRetrievalService>();

        // Scoped, so the pass ordinal in the [retrieval] log line counts within one turn. Note the
        // recipe engine resolves retrieval from a fresh scope of its own, so its ordinals restart at
        // one - they are scope-local, not turn-global.
        services.AddScoped<RetrievalCallCounter>();

        var bgOptions = configuration
            .GetSection(BackgroundServiceOptions.SectionName)
            .Get<BackgroundServiceOptions>() ?? new BackgroundServiceOptions();

        if (bgOptions.KnowledgeIndexStartup)
            services.AddHostedService<KnowledgeIndexStartupService>();

        // Registered after the sync service so the index is current before the sessions are built.
        // It is a BackgroundService, so it does not hold up the host either way.
        // Deliberately NOT behind a BackgroundServices flag, unlike every other hosted service here.
        // The sessions are per-process state, so every instance needs its own warm pair - pinning this
        // to one instance would leave the others cold without saving the work. It is not free (the
        // first ONNX build downloads the model files onto that instance's own disk, and on a host
        // without ONNX the warm-up embed is one small call to the remote provider), but that cost is
        // per instance by nature and a pinning flag cannot avoid it. An opt-out for memory-capped
        // hosts already exists in KnowledgeIndexConstants.WarmupEnabledConfigKey; a second switch
        // would only create two controls with unclear precedence.
        services.AddHostedService<OnnxWarmupService>();

        // Same reasoning as the warm-up above: the sessions are per-process state, so every instance
        // has to release its own. Injecting IEnumerable<IUnloadableInferenceSession> is safe from a
        // singleton because every registration of that interface above is itself a singleton; on a host
        // without ONNX the sequence is empty and the service returns without starting a timer.
        services.AddHostedService<OnnxSessionIdleUnloadService>();
    }

    internal static bool IsOnnxRuntimeSupported(IConfiguration configuration)
    {
        var configured = configuration[KnowledgeIndexConstants.OnnxEnabledConfigKey];
        if (bool.TryParse(configured, out var enabled))
        {
            return enabled;
        }

        // ONNX Runtime 1.20.1's bundled cpuinfo could not detect the Snapdragon X SoC on Windows ARM64
        // and faulted the process when an InferenceSession was created, so ONNX used to be disabled
        // there. That no longer reproduces on the 1.29.0 runtime this project ships: opening a session
        // AND running a forward pass both succeed on Windows ARM64 (see
        // OnnxRuntimePlatformProbeTests in Klacks.IntegrationTest, which is the way to re-check this on
        // any new platform). Keeping the block would silently downgrade every ARM host to a remote
        // embedding API, and ARM servers are becoming ordinary deployment targets.
        return true;
    }

    // Internal so the unit tests can pin the default and the parsing without building the container.
    internal static bool ResolveOnnxAllowIntraOpSpinning(IConfiguration configuration) =>
        bool.TryParse(configuration[KnowledgeIndexConstants.OnnxAllowIntraOpSpinningConfigKey], out var allow)
            ? allow
            : KnowledgeIndexConstants.DefaultOnnxAllowIntraOpSpinning;

    private static string ResolveSnapshotFile(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration[KnowledgeIndexConstants.SnapshotFileConfigKey];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(environment.ContentRootPath, configured);
        }

        return Path.Combine(environment.ContentRootPath, KnowledgeIndexConstants.SnapshotFileRelativePath);
    }

    private static string ResolveModelsRoot(IConfiguration configuration)
    {
        var configured = configuration[KnowledgeIndexConstants.ModelsRootConfigKey];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        return Path.Combine(AppContext.BaseDirectory, KnowledgeIndexConstants.ModelsCacheSubdirectory);
    }
}
