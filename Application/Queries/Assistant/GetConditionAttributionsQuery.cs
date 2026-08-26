// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetConditionAttributionsQuery : IRequest<IReadOnlyList<ConditionAttributionDto>>
{
    public string UserId { get; set; } = string.Empty;

    public IReadOnlyList<Guid> EntityIds { get; set; } = [];
}
