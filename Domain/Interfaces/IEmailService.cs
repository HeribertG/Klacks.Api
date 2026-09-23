// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces;

public interface IEmailService
{
    string SendMail(string email, string title, string message);
    string SendReplyMail(string email, string title, string message, IReadOnlyDictionary<string, string> headers);
    Task<bool> CanSendEmailAsync();
}
