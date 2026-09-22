// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Email;

public interface IEmailIntentAnalysisService
{
    Task<InboundAnalysis?> AnalyzeAsync(ReceivedEmail email, CancellationToken cancellationToken = default);
}
