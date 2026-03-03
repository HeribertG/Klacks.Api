// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IImapEmailService
{
    Task<List<ReceivedEmail>> FetchNewEmailsAsync(CancellationToken cancellationToken = default);
}
