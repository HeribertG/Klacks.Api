// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Accounts;

public interface IAccountNotificationService
{
    Task<string> SendEmailAsync(string title, string email, string message);
}