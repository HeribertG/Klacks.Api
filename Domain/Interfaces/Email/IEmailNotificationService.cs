// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailNotificationService
{
    Task NotifyNewEmailsAsync(int count);
    Task NotifyReadStateChangedAsync(string userId, Guid emailId, bool isRead, string folder);
}
