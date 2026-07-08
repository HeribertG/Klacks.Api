// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailIntentAnalysisService
{
    Task<EmailAnalysis?> AnalyzeAsync(ReceivedEmail email, CancellationToken cancellationToken = default);
}
