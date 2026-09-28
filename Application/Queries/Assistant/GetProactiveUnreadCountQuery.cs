// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetProactiveUnreadCountQuery : IRequest<int>
{
    public string UserId { get; set; } = string.Empty;
}
