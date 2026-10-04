// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailClientAssignmentService
{
    Task AssignInboxEmailsToClientsAsync();
    Task AssignNewEmailAsync(ReceivedEmail email);
    Task ReassignOrphanedEmailsAsync();
    /// <summary>
    /// The single client owning the sender address, or null when no client or more than one client owns it -
    /// inbound automation must never act on a guessed employee.
    /// </summary>
    Task<(Guid ClientId, EntityTypeEnum ClientType)?> ResolveClientAsync(ReceivedEmail email, CancellationToken cancellationToken = default);
    Task<string?> GetStoredAddressAsync(Guid clientId, string fromAddress, CancellationToken cancellationToken = default);
}
