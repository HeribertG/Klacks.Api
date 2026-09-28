// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetLLMUsageQuery : IRequest<LLMUsageResponse>
{
    public string UserId { get; set; } = string.Empty;
    public int Days { get; set; } = 30;
}