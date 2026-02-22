// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class DeleteProviderCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public DeleteProviderCommand(Guid id)
    {
        Id = id;
    }
}