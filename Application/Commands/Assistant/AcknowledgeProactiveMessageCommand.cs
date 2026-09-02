// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class AcknowledgeProactiveMessageCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;
}
