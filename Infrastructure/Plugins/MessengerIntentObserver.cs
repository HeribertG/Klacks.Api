// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Entry point of the messenger intent analysis: receives every inbound messenger message resolved to
/// a known CLIENT (employee, extern employee, customer) and only enqueues it into IMessengerIntentQueue.
/// The analysis itself (MessengerIntentProcessor, driven by MessengerIntentBackgroundService) runs
/// decoupled, because this observer is called synchronously from MessagingService.PersistInboundAsync
/// inside the plugin's webhook request / Slack poll loop and an inline LLM call would block it.
/// Distinct from MessagingPluginInboundMessageObserver, which reacts to messages resolved to an APP USER
/// (escalation replies) — the two paths never overlap because a message resolves to exactly one or the
/// other.
/// Depends only on the queue (a dependency leaf) and the logger on purpose: MessagingService receives
/// this observer through its constructor, and MessagingService itself sits deep inside the LLM/trigger
/// graph (ILLMService -> ... -> IOfflineMessengerNotifier -> MessagingService). Injecting the kernel
/// services here closed that graph into a cycle and stopped the host from booting whenever the
/// messaging plugin was active.
/// </summary>
/// <param name="queue">Bounded in-memory queue drained by MessengerIntentBackgroundService</param>
/// <param name="logger">Structured log of enqueue decisions</param>

using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public sealed class MessengerIntentObserver : IInboundClientMessengerObserver
{
    private readonly IMessengerIntentQueue _queue;
    private readonly ILogger<MessengerIntentObserver> _logger;

    public MessengerIntentObserver(IMessengerIntentQueue queue, ILogger<MessengerIntentObserver> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public Task OnInboundMessageAsync(InboundClientMessengerMessage message, CancellationToken cancellationToken = default)
    {
        if (_queue.TryEnqueue(message))
        {
            _logger.LogDebug(
                "Queued messenger message {MessageId} of client {ClientId} for intent analysis",
                message.MessageId, message.ClientId);
        }

        return Task.CompletedTask;
    }
}
