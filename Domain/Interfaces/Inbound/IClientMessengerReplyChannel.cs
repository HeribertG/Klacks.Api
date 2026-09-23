// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Core-owned contract through which the inbound clarification dialog reaches a CLIENT privately on a
/// messenger. The core never names messaging-plugin types; the adapter
/// (MessagingPluginClientReplyChannel) does, mirroring IOfflineMessengerNotifier. The recipient is always
/// the client's stored personal MessengerContact of the given messenger type and only when the provider
/// classifies it as a personal address. Implementations must stay dependency leaves of plugin-level
/// services: MessagingService sits inside the ILLMService graph (see messaging-observer-di-leaf).
/// </summary>
/// <param name="clientId">Client whose stored contact is looked up</param>
/// <param name="channel">Messenger type name, e.g. "Telegram", "Slack", "Line"</param>
/// <param name="recipient">A recipient returned by ResolvePersonalRecipientAsync</param>
/// <param name="text">Plain text to send</param>

using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IClientMessengerReplyChannel
{
    Task<string?> ResolvePersonalRecipientAsync(Guid clientId, string channel, CancellationToken cancellationToken = default);

    Task<InboundReplyResult> SendAsync(string channel, string recipient, string text, CancellationToken cancellationToken = default);
}
