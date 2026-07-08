// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailAnalysisRepository
{
    Task AddAsync(EmailAnalysis analysis, CancellationToken cancellationToken = default);

    Task<EmailAnalysis?> GetByReceivedEmailIdAsync(Guid receivedEmailId, CancellationToken cancellationToken = default);
}
