// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Factory for creating messaging provider adapters based on provider type.
/// </summary>
/// <param name="providerType">The provider type constant from MessagingConstants</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Messaging;
using Klacks.Api.Infrastructure.Services.Messaging.Providers;

namespace Klacks.Api.Infrastructure.Services.Messaging;

public class MessagingProviderAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;

    public MessagingProviderAdapterFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IMessagingProviderAdapter Create(string providerType)
    {
        return providerType switch
        {
            MessagingConstants.ProviderTelegram => _serviceProvider.GetRequiredService<TelegramMessagingProvider>(),
            MessagingConstants.ProviderWhatsApp => _serviceProvider.GetRequiredService<WhatsAppMessagingProvider>(),
            MessagingConstants.ProviderSignal => _serviceProvider.GetRequiredService<SignalMessagingProvider>(),
            MessagingConstants.ProviderSms => _serviceProvider.GetRequiredService<SmsMessagingProvider>(),
            _ => throw new ArgumentException($"Unknown messaging provider type: {providerType}")
        };
    }
}
