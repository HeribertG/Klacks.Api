// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailClientAssignmentService
{
    Task AssignInboxEmailsToClientsAsync();
    Task AssignNewEmailAsync(ReceivedEmail email);
    Task ReassignOrphanedEmailsAsync();
    Task<(Guid ClientId, EntityTypeEnum ClientType)?> ResolveClientAsync(ReceivedEmail email, CancellationToken cancellationToken = default);
}
