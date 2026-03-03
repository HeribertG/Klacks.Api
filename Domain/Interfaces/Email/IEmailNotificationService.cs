// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailNotificationService
{
    Task NotifyNewEmailsAsync(int count);
    Task NotifyReadStateChangedAsync(Guid emailId, bool isRead, string folder);
}
