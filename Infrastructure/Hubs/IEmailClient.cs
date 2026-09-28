// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Infrastructure.Hubs;

public interface IEmailClient
{
    Task NewEmailsReceived(NewEmailsNotificationDto notification);
    Task EmailReadStateChanged(EmailReadStateNotificationDto notification);
}
