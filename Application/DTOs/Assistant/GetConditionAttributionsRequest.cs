// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class GetConditionAttributionsRequest
{
    public IReadOnlyList<Guid> EntityIds { get; set; } = [];
}
