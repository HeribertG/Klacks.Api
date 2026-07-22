// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface IReadOnlyResearchService
{
    Task<ReadOnlyResearchResult> ResearchAsync(
        string question,
        SkillExecutionContext context,
        CancellationToken cancellationToken = default);
}
