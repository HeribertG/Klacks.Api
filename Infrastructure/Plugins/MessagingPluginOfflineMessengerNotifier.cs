// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the core's IOfflineMessengerNotifier to the messaging plugin. This is the only file in the
/// dispatch path that names plugin types, mirroring the existing *Bridge classes in this folder;
/// AgentTriggerService itself stays free of them. The plugin's own runtime switch is honoured through
/// IPluginStateChecker, the same gate every messaging controller uses via RequireFeaturePluginAttribute,
/// so a disabled plugin yields ChannelUnavailable instead of an outbound message.
/// The recipient is resolved through the preferred UserMessengerContact, which is the only lookup that
/// maps an AppUser to a messenger identity - MessengerContact hangs off a non-nullable ClientId and can
/// never resolve a planner. Every failure path is swallowed into a result value on purpose: a broken
/// plugin must not abort the trigger loop that still has inbox rows to write for other recipients.
/// </summary>
/// <param name="pluginStateChecker">Tells whether the messaging plugin is currently enabled.</param>
/// <param name="contactRepository">Resolves the recipient's preferred messenger identity.</param>
/// <param name="messagingService">Sends the outbound message; needs no HttpContext and no rights check.</param>
/// <param name="logger">Structured log for lookups and sends that did not work out.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Plugin.Contracts;
using Klacks.Plugin.Messaging.Application.Constants;
using Klacks.Plugin.Messaging.Application.Interfaces;
using Klacks.Plugin.Messaging.Domain.Interfaces;
using Klacks.Plugin.Messaging.Domain.Models;

namespace Klacks.Api.Infrastructure.Plugins;

public class MessagingPluginOfflineMessengerNotifier : IOfflineMessengerNotifier
{
    private readonly IPluginStateChecker _pluginStateChecker;
    private readonly IUserMessengerContactRepository _contactRepository;
    private readonly IMessagingService _messagingService;
    private readonly ILogger<MessagingPluginOfflineMessengerNotifier> _logger;

    public MessagingPluginOfflineMessengerNotifier(
        IPluginStateChecker pluginStateChecker,
        IUserMessengerContactRepository contactRepository,
        IMessagingService messagingService,
        ILogger<MessagingPluginOfflineMessengerNotifier> logger)
    {
        _pluginStateChecker = pluginStateChecker;
        _contactRepository = contactRepository;
        _messagingService = messagingService;
        _logger = logger;
    }

    public async Task<OfflineMessengerDeliveryResult> TrySendAsync(
        string userId,
        string message,
        string triggerKind,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(message))
        {
            return OfflineMessengerDeliveryResult.NoContact;
        }

        if (!_pluginStateChecker.IsEnabled(MessagingConstants.PluginName))
        {
            return OfflineMessengerDeliveryResult.ChannelUnavailable;
        }

        UserMessengerContact? contact;
        try
        {
            contact = await _contactRepository.GetPreferredAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Messenger contact lookup failed for user {UserId} on trigger {TriggerKind}; no loud channel this round",
                userId,
                triggerKind);
            return OfflineMessengerDeliveryResult.ChannelUnavailable;
        }

        if (contact == null || string.IsNullOrWhiteSpace(contact.Value))
        {
            return OfflineMessengerDeliveryResult.NoContact;
        }

        // MessagingService.ResolveProviderAsync matches the provider name first and the provider type
        // second, both case-insensitively, and MessengerType names are exactly the stored provider
        // types - so the enum name is a valid selector and no second mapping table is needed here.
        var channel = contact.Type.ToString();
        try
        {
            var result = await _messagingService.SendMessageAsync(
                channel,
                new SendMessageRequest(contact.Value, message),
                cancellationToken);

            if (result.Success)
            {
                return OfflineMessengerDeliveryResult.Sent(channel);
            }

            return result.IsThrottled
                ? OfflineMessengerDeliveryResult.Throttled(channel, result.ErrorMessage)
                : OfflineMessengerDeliveryResult.Failed(channel, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            return OfflineMessengerDeliveryResult.Failed(channel, ex.Message);
        }
    }
}
