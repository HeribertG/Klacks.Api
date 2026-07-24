// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class MarkAllProactiveMessagesReadCommand : IRequest
{
    public string UserId { get; set; } = string.Empty;
}
