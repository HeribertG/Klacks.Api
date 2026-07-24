// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class SetProactiveReactionCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ProactiveReaction Reaction { get; set; }
}
