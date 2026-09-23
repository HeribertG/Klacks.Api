// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the core's IClientMessengerReplyChannel to the messaging plugin, the only file of the
/// clarification dialog that names plugin types (like MessagingPluginOfflineMessengerNotifier). The
/// recipient is the client's stored MessengerContact of the same messenger type, never the chat the
/// message came from, and only when the enabled provider's adapter implements IPersonalRecipientClassifier
/// and classifies the value as a personal address; a provider without that capability counts as "no".
/// The channel name (MessengerType name) matches the provider type case-insensitively ("Line" vs
/// "LINE"). The plugin's runtime switch is honoured through IPluginStateChecker. Every failure is mapped
/// to a result value. Constructor takes plugin-level services only: MessagingService sits inside the
/// ILLMService graph, so this class must stay a dependency leaf and must never be injected into a
/// messenger observer.
/// </summary>
/// <param name="pluginStateChecker">Tells whether the messaging plugin is enabled</param>
/// <param name="contactRepository">Resolves the client's stored messenger contact</param>
/// <param name="providerRepository">Lists the enabled providers</param>
/// <param name="adapterFactory">Creates the provider adapter for the personal-address check</param>
/// <param name="messagingService">Sends and records the outbound message</param>
/// <param name="logger">Logs lookups and sends that did not work out</param>

using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Plugin.Contracts;
using Klacks.Plugin.Messaging.Application.Constants;
using Klacks.Plugin.Messaging.Application.Interfaces;
using Klacks.Plugin.Messaging.Domain.Enums;
using Klacks.Plugin.Messaging.Domain.Interfaces;
using Klacks.Plugin.Messaging.Domain.Models;

namespace Klacks.Api.Infrastructure.Plugins;

public sealed class MessagingPluginClientReplyChannel : IClientMessengerReplyChannel
{
    private const string PluginDisabledError = "The messaging plugin is disabled";

    private readonly IPluginStateChecker _pluginStateChecker;
    private readonly IMessengerContactRepository _contactRepository;
    private readonly IMessagingProviderRepository _providerRepository;
    private readonly IMessagingProviderAdapterFactory _adapterFactory;
    private readonly IMessagingService _messagingService;
    private readonly ILogger<MessagingPluginClientReplyChannel> _logger;

    public MessagingPluginClientReplyChannel(
        IPluginStateChecker pluginStateChecker,
        IMessengerContactRepository contactRepository,
        IMessagingProviderRepository providerRepository,
        IMessagingProviderAdapterFactory adapterFactory,
        IMessagingService messagingService,
        ILogger<MessagingPluginClientReplyChannel> logger)
    {
        _pluginStateChecker = pluginStateChecker;
        _contactRepository = contactRepository;
        _providerRepository = providerRepository;
        _adapterFactory = adapterFactory;
        _messagingService = messagingService;
        _logger = logger;
    }

    public async Task<string?> ResolvePersonalRecipientAsync(Guid clientId, string channel, CancellationToken cancellationToken = default)
    {
        if (!_pluginStateChecker.IsEnabled(MessagingConstants.PluginName)
            || !Enum.TryParse<MessengerType>(channel, ignoreCase: true, out var messengerType))
        {
            return null;
        }

        try
        {
            var contact = await _contactRepository.GetByClientAndTypeAsync(clientId, messengerType, cancellationToken);
            if (contact == null || string.IsNullOrWhiteSpace(contact.Value))
            {
                return null;
            }

            var providers = await _providerRepository.GetEnabledAsync();
            var provider = providers.FirstOrDefault(p => string.Equals(p.ProviderType, channel, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                return null;
            }

            var adapter = _adapterFactory.Create(provider.ProviderType);
            return adapter is IPersonalRecipientClassifier classifier && classifier.IsPersonalRecipient(contact.Value)
                ? contact.Value
                : null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Personal messenger contact lookup failed for client {ClientId} on {Channel}", clientId, channel);
            return null;
        }
    }

    public async Task<InboundReplyResult> SendAsync(string channel, string recipient, string text, CancellationToken cancellationToken = default)
    {
        if (!_pluginStateChecker.IsEnabled(MessagingConstants.PluginName))
        {
            return InboundReplyResult.Failed(PluginDisabledError);
        }

        try
        {
            var result = await _messagingService.SendMessageAsync(
                channel,
                new SendMessageRequest(recipient, text, SenderDisplayName: MessagingConstants.KlacksySenderDisplayName),
                cancellationToken);

            return result.Success ? InboundReplyResult.Sent : InboundReplyResult.Failed(result.ErrorMessage);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Clarification message on {Channel} could not be sent", channel);
            return InboundReplyResult.Failed(ex.Message);
        }
    }
}
