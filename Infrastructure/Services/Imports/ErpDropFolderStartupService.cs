// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs ErpDropFolderInitializer once at startup, so the ERP drop folder exists before anyone opens the Import card
/// or asks Klacksy for it. The initializer is scoped (repository), hence the short-lived scope. A failure is logged
/// and startup continues; a slow drop point read delays startup by that read.
/// </summary>
/// <param name="serviceScopeFactory">Creates the scope the initializer is resolved from</param>
/// <param name="logger">Reports a failed startup check</param>

using Klacks.Api.Application.Services.Imports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Imports;

public sealed class ErpDropFolderStartupService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<ErpDropFolderStartupService> _logger;

    public ErpDropFolderStartupService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ErpDropFolderStartupService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var initializer = scope.ServiceProvider.GetRequiredService<ErpDropFolderInitializer>();
            await initializer.EnsureFoldersAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "ERP drop folder startup check failed - skipping, startup continues");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
