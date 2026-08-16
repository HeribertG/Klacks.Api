// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A second, independent IInboundMessengerObserver alongside MessagingPluginInboundMessageObserver
/// (that one is deliberately transport-only per its own XML doc; DI resolves every registered
/// observer, see MessagingService.NotifyInboundObserversAsync). This one is the interpretation this
/// build adds: a short "I'm taking it" reply resolves the sender's currently-Notified escalation
/// stage. A reply that is not an acknowledgement, or arrives from a user with no Notified stage, is
/// silently ignored here - not every message from a paired user is about an escalation.
/// </summary>
/// <param name="chainService">Owns the conditional acknowledge transition and the handoff notification.</param>
/// <param name="logger">Logs a resolved acknowledgement; a miss is not logged, it would fire on every ordinary reply.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Application.Services.Assistant.Escalation;

public sealed class EscalationReplyObserver : IInboundMessengerObserver
{
    private readonly IEscalationChainService _chainService;
    private readonly ILogger<EscalationReplyObserver> _logger;

    public EscalationReplyObserver(IEscalationChainService chainService, ILogger<EscalationReplyObserver> logger)
    {
        _chainService = chainService;
        _logger = logger;
    }

    public async Task OnInboundMessageAsync(InboundMessengerMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null || !EscalationAcknowledgementDetector.IsAcknowledgement(message.Content))
        {
            return;
        }

        var acknowledged = await _chainService.AcknowledgeAsync(message.UserId, cancellationToken);
        if (acknowledged)
        {
            _logger.LogInformation(
                "Escalation stage acknowledged by user {UserId} via message {MessageId}", message.UserId, message.MessageId);
        }
    }
}
