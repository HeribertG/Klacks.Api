// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bounded in-memory queue that decouples messenger intent analysis from the inbound call. Registered
/// as a singleton and deliberately a dependency leaf (logger only): MessagingService receives
/// MessengerIntentObserver, which receives this queue, so any further dependency here would re-open the
/// DI cycle MessagingService -> observer -> kernel -> ILLMService -> ... -> MessagingService.
/// The channel uses FullMode.Wait so that TryWrite reports a full queue as false instead of silently
/// dropping (DropWrite/DropOldest would return true); the caller is never blocked either way. A dropped
/// message is logged with its MessageId and is not analyzed.
/// Accepted trade-off: the queue has no persistence. Messages still queued when the process stops or
/// crashes are never analyzed; the messaging plugin deduplicates on ExternalMessageId, so the provider
/// re-delivering the same message does not re-ingest it either.
/// </summary>
/// <param name="logger">Logs every message dropped because the queue is full</param>

using System.Threading.Channels;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public sealed class MessengerIntentQueue : IMessengerIntentQueue
{
    internal const int Capacity = 500;

    private readonly ILogger<MessengerIntentQueue> _logger;
    private readonly Channel<InboundClientMessengerMessage> _channel;

    public MessengerIntentQueue(ILogger<MessengerIntentQueue> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<InboundClientMessengerMessage>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public bool TryEnqueue(InboundClientMessengerMessage message)
    {
        if (_channel.Writer.TryWrite(message))
        {
            return true;
        }

        _logger.LogWarning(
            "Messenger intent queue is full ({Capacity}); dropping message {MessageId} of client {ClientId} without analysis",
            Capacity, message.MessageId, message.ClientId);
        return false;
    }

    public IAsyncEnumerable<InboundClientMessengerMessage> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}
