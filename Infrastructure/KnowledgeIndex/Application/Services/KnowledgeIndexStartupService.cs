// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Application.Services;

/// <summary>
/// Runs the knowledge index synchronization after application startup.
/// Failures are logged but do not block startup — the chat flow degrades gracefully
/// to the Tier2 LLM classifier when the index is unavailable.
/// </summary>
/// <param name="synchronizer">Service that builds and maintains the knowledge_index table.</param>
/// <param name="logger">Logger for startup diagnostics and error reporting.</param>
public sealed class KnowledgeIndexStartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<KnowledgeIndexStartupService> _logger;

    public KnowledgeIndexStartupService(
        IServiceProvider serviceProvider,
        ILogger<KnowledgeIndexStartupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var synchronizer = scope.ServiceProvider.GetRequiredService<IKnowledgeIndexSynchronizer>();
            await synchronizer.SyncAsync(ct);
            _logger.LogInformation("Knowledge index sync completed at startup.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Knowledge index sync failed at startup. Chat will fall back to Tier2 classifier.");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
