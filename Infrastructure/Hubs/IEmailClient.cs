// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IEmailClient
{
    Task NewEmailsReceived(NewEmailsNotificationDto notification);
    Task EmailReadStateChanged(EmailReadStateNotificationDto notification);
}
