// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The return path of MessagingPluginOfflineMessengerNotifier. That class carries a message from the
/// core out to a user's messenger; this one is the core's ear for the answer. The contract it
/// implements lives in Klacks.Plugin.Contracts rather than in the core because the project reference
/// runs Klacks.Api -> Klacks.Plugin.Messaging, so a core-owned interface would be invisible to the
/// plugin that has to call it - the exact opposite of the outbound direction, where the plugin type
/// is the one being named here.
/// This observer only records that an answer arrived. Reading what the answer says, deciding whether
/// it settles anything and driving an escalation forward are separate concerns and deliberately not
/// built here: an observer that silently interpreted content would make the transport path and the
/// decision path impossible to change independently.
/// </summary>
/// <param name="logger">Structured log of the inbound message and the user it came from.</param>

using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public sealed class MessagingPluginInboundMessageObserver : IInboundMessengerObserver
{
    private readonly ILogger<MessagingPluginInboundMessageObserver> _logger;

    public MessagingPluginInboundMessageObserver(ILogger<MessagingPluginInboundMessageObserver> logger)
    {
        _logger = logger;
    }

    public Task OnInboundMessageAsync(InboundMessengerMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Messenger reply {MessageId} received from user {UserId} on channel {Channel} at {ReceivedAt}",
            message.MessageId,
            message.UserId,
            message.Channel,
            message.ReceivedAt);

        return Task.CompletedTask;
    }
}
