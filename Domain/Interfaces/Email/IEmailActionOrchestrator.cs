// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailActionOrchestrator
{
    Task<EmailActionOutcome?> ExecuteAsync(ReceivedEmail email, EmailAnalysis analysis, CancellationToken cancellationToken = default);
}
