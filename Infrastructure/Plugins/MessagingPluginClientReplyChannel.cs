// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the core's IClientMessengerReplyChannel to the messaging plugin, the only file of the
/// clarification dialog that names plugin types (like MessagingPluginOfflineMessengerNotifier). The
/// recipient is the client's stored MessengerContact of the same messenger type, never the chat the
/// message came from, and only when the enabled provider's adapter implements IPersonalRecipientClassifier
/// and classifies the value as a personal address; a provider without that capability counts as "no".
/// The channel name (MessengerType name) matches the provider type case-insensitively ("Line" vs
/// "LINE"). Exactly one enabled provider must match that type; zero or several are treated as
/// "cannot tell" (fail-closed). MessagingService.SendMessageAsync resolves its providerName argument by
/// NAME first and only falls back to a type match among enabled providers when no provider carries that
/// exact name (ResolveProviderAsync); passing the raw channel string there could therefore hit a
/// differently-typed provider that happens to be named after the channel, or - among several enabled
/// providers of the classified type - a different one than the adapter here just approved. SendAsync
/// resolves and re-classifies the provider itself and always sends through its NAME, so it goes through
/// the exact provider that was classified as personal.
/// The plugin's runtime switch is honoured through IPluginStateChecker. Every failure is mapped
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
    private const string NoVerifiedPersonalProviderError = "No single enabled provider could verify the recipient as a personal address";

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
            || !TryParseChannel(channel, out var messengerType))
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

            var resolved = await ResolveEnabledProviderAsync(messengerType);
            return resolved is { Classifier: { } classifier } && classifier.IsPersonalRecipient(contact.Value)
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

        if (!TryParseChannel(channel, out var messengerType))
        {
            return InboundReplyResult.Failed(NoVerifiedPersonalProviderError);
        }

        try
        {
            var resolved = await ResolveEnabledProviderAsync(messengerType);
            if (resolved is not { Provider: var provider, Classifier: { } classifier } || !classifier.IsPersonalRecipient(recipient))
            {
                return InboundReplyResult.Failed(NoVerifiedPersonalProviderError);
            }

            var result = await _messagingService.SendMessageAsync(
                provider.Name,
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

    private static bool TryParseChannel(string channel, out MessengerType messengerType) =>
        Enum.TryParse(channel, ignoreCase: true, out messengerType) && Enum.IsDefined(messengerType);

    private async Task<(MessagingProvider Provider, IPersonalRecipientClassifier? Classifier)?> ResolveEnabledProviderAsync(MessengerType type)
    {
        var providers = await _providerRepository.GetEnabledAsync();
        var typeName = type.ToString();
        var matches = providers
            .Where(p => p.IsEnabled && string.Equals(p.ProviderType, typeName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count != 1)
        {
            return null;
        }

        var provider = matches[0];
        var adapter = _adapterFactory.Create(provider.ProviderType);
        return (provider, adapter as IPersonalRecipientClassifier);
    }
}
