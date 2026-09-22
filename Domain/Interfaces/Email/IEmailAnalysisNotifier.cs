// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailAnalysisNotifier
{
    Task NotifyAsync(
        ReceivedEmail email,
        InboundAnalysis analysis,
        EmailActionOutcome? actionOutcome = null,
        string? periodLoadSummary = null,
        CancellationToken cancellationToken = default);
}
