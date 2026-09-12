// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answers whether the inbox page exists for this installation. The Angular InboxGuard lets the page
/// open only when an incoming mail server, a user name and a password are configured, so anything that
/// offers the page has to ask the same question or it offers a page the guard then refuses.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IInboxAvailabilityService
{
    /// <param name="cancellationToken">Cancels the settings lookup</param>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
