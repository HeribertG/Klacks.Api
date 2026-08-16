// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A second, independent IInboundMessengerObserver alongside MessagingPluginInboundMessageObserver
/// (that one is deliberately transport-only per its own XML doc; DI resolves every registered
/// observer, see MessagingService.NotifyInboundObserversAsync). This one is the interpretation this
/// build adds: a short "I'm taking it" reply resolves the sender's currently-Notified escalation
/// stage. A reply that is not an acknowledgement, or arrives from a user with no Notified stage, is
/// silently ignored here - not every message from a paired user is about an escalation.
/// </summary>
/// <param name="serviceProvider">Resolves IEscalationChainService lazily, not in the constructor - see
/// the remark above on why.</param>
/// <param name="logger">Logs a resolved acknowledgement; a miss is not logged, it would fire on every ordinary reply.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Plugin.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Application.Services.Assistant.Escalation;

public sealed class EscalationReplyObserver : IInboundMessengerObserver
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EscalationReplyObserver> _logger;

    public EscalationReplyObserver(IServiceProvider serviceProvider, ILogger<EscalationReplyObserver> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task OnInboundMessageAsync(InboundMessengerMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null || !EscalationAcknowledgementDetector.IsAcknowledgement(message.Content))
        {
            return;
        }

        // Resolved here, not injected via the constructor: IEscalationChainService -> IEscalationNotifier
        // -> IOfflineMessengerNotifier -> IMessagingService -> IEnumerable<IInboundMessengerObserver>
        // closes a cycle back to this class if resolved eagerly. Deferring the lookup to call time keeps
        // this observer's own construction (and therefore IMessagingService's) acyclic; by the time a
        // message actually arrives, IMessagingService already exists, so no cycle is walked again.
        var chainService = _serviceProvider.GetRequiredService<IEscalationChainService>();

        var acknowledged = await chainService.AcknowledgeAsync(message.UserId, cancellationToken);
        if (acknowledged)
        {
            _logger.LogInformation(
                "Escalation stage acknowledged by user {UserId} via message {MessageId}", message.UserId, message.MessageId);
        }
    }
}
