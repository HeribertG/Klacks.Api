// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailActionOrchestrator
{
    Task<EmailActionOutcome?> ExecuteAsync(ReceivedEmail email, InboundAnalysis analysis, CancellationToken cancellationToken = default);
}
