// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class RemoveGroupItemByClientAndGroupCommand : IRequest<bool>
{
    public Guid ClientId { get; set; }
    public Guid GroupId { get; set; }
}
