// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces;

public interface IEmailService
{
    string SendMail(string email, string title, string message);

    /// <summary>
    /// Sends an email reply with the given threading headers over the configured SMTP account.
    /// </summary>
    /// <param name="email">The single recipient address</param>
    /// <param name="title">The subject line</param>
    /// <param name="message">The plain-text body</param>
    /// <param name="headers">Extra reply headers such as In-Reply-To, References and Auto-Submitted</param>
    /// <returns>The success marker, or an error message when sending failed</returns>
    /// <exception cref="FormatException">Thrown when the recipient address, subject or a header value is
    /// invalid (e.g. more than one address, or a value containing a line break). Callers must catch this;
    /// it is not mapped to a failed result.</exception>
    string SendReplyMail(string email, string title, string message, IReadOnlyDictionary<string, string> headers);
    Task<bool> CanSendEmailAsync();
}
