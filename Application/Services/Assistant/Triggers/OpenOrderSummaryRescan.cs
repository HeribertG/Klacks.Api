// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IOpenOrderSummaryRescan. Runs OpenOrderDetector through IAgentTriggerDetectorRunner inside a DI scope
/// of its own: the caller is the ERP import, whose scoped context still tracks the import's entities, and the
/// ledger repository commits on that context per transition - sharing it could flush import state that is not
/// meant to be saved, or collide with an ambient transaction. The fresh scope also gives the detector its own
/// scoped ICompanyClock instead of borrowing one across scopes. Awaited by the caller, never fire-and-forget.
/// </summary>
/// <param name="scopeFactory">Creates the scope the detector, the runner and their repositories live in.</param>
/// <param name="logger">Logs a failed rescan; the import that triggered it is not affected.</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class OpenOrderSummaryRescan : IOpenOrderSummaryRescan
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OpenOrderSummaryRescan> _logger;

    public OpenOrderSummaryRescan(IServiceScopeFactory scopeFactory, ILogger<OpenOrderSummaryRescan> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var detector = scope.ServiceProvider.GetRequiredService<OpenOrderDetector>();
            var runner = scope.ServiceProvider.GetRequiredService<IAgentTriggerDetectorRunner>();

            var outcome = await runner.RunAsync(detector, cancellationToken);

            _logger.LogInformation(
                "Open-order summary rescan after ERP import: {Events} event(s), {Resolved} resolved",
                outcome.Events, outcome.Resolved);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Open-order summary rescan after ERP import failed; the hourly tick reports the orders instead");
        }
    }
}
