// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetProactiveUnreadCountQuery : IRequest<int>
{
    public string UserId { get; set; } = string.Empty;
}
