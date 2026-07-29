// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetGoalCandidatesQuery : IRequest<IReadOnlyList<GoalCandidateDto>>
{
    public string UserId { get; set; } = string.Empty;

    public string? Status { get; set; }

    public int? Take { get; set; }
}
