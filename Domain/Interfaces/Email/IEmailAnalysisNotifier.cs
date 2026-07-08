// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailAnalysisNotifier
{
    Task NotifyAsync(
        ReceivedEmail email,
        EmailAnalysis analysis,
        EmailActionOutcome? actionOutcome = null,
        string? periodLoadSummary = null,
        CancellationToken cancellationToken = default);
}
