// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Plugin.Contracts;

namespace Klacks.Api.Application.Interfaces.Plugins;

/// <summary>
/// In-process hand-off between the messenger ingest path (MessengerIntentObserver, called inside the
/// plugin's webhook request / poll loop) and the background consumer that runs the intent-analysis
/// kernel. Implementations must stay a dependency leaf: MessagingService receives the observer, and the
/// observer receives this queue, so anything this queue depends on becomes part of MessagingService's
/// own constructor graph.
/// </summary>
public interface IMessengerIntentQueue
{
    /// <summary>
    /// Enqueues a message without ever blocking the caller. Returns false (and logs the MessageId) when
    /// the queue is full; the message is then dropped and not analyzed.
    /// </summary>
    bool TryEnqueue(InboundClientMessengerMessage message);

    /// <summary>
    /// Streams the queued messages to the single background consumer until the token is cancelled.
    /// </summary>
    IAsyncEnumerable<InboundClientMessengerMessage> ReadAllAsync(CancellationToken cancellationToken);
}
