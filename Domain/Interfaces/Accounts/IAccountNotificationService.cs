// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Accounts;

public interface IAccountNotificationService
{
    Task<string> SendEmailAsync(string title, string email, string message);
}