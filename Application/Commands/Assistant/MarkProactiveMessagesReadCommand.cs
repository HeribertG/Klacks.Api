// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class MarkProactiveMessagesReadCommand : IRequest
{
    public string UserId { get; set; } = string.Empty;

    public IReadOnlyList<Guid> Ids { get; set; } = [];
}
