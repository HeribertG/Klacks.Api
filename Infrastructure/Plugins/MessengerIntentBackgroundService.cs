// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single consumer of IMessengerIntentQueue: drains the queued messenger messages and runs each one
/// through IMessengerIntentProcessor in its own fresh DI scope, so the LLM call no longer blocks the
/// messaging plugin's webhook response or Slack poll loop. A failure while processing one message is
/// logged and the loop continues with the next one - an exception escaping ExecuteAsync would stop the
/// host. Registered only when BackgroundServices:MessengerIntentAnalysis is on.
/// Accepted trade-off: messages still queued (or mid-analysis) when the host stops or crashes are not
/// analyzed; the messaging plugin deduplicates on ExternalMessageId, so they are not re-ingested either.
/// </summary>
/// <param name="queue">In-memory queue filled by MessengerIntentObserver</param>
/// <param name="scopeFactory">Creates the per-message scope the processor is resolved from</param>
/// <param name="logger">Lifecycle and per-message failure log</param>

using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public sealed class MessengerIntentBackgroundService : BackgroundService
{
    private readonly IMessengerIntentQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MessengerIntentBackgroundService> _logger;

    public MessengerIntentBackgroundService(
        IMessengerIntentQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<MessengerIntentBackgroundService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Messenger intent analysis consumer started");

        try
        {
            await foreach (var message in _queue.ReadAllAsync(stoppingToken))
            {
                await ProcessMessageAsync(message, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Messenger intent analysis consumer stopped");
    }

    private async Task ProcessMessageAsync(InboundClientMessengerMessage message, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<IMessengerIntentProcessor>();
            await processor.ProcessAsync(message, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Messenger intent analysis failed for message {MessageId} of client {ClientId}; message is not retried",
                message.MessageId, message.ClientId);
        }
    }
}
