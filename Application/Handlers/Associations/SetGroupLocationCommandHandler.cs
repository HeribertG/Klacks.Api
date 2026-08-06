// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stores a group's coordinates. This needs its own endpoint rather than the generic group PUT:
/// GroupResource carries no latitude or longitude at all, and the write additionally marks the group as
/// geocoded — a flag the caller has no business setting by hand.
/// </summary>
/// <param name="groupRepository">Owns the coordinate write and the geocoded marker</param>
/// <param name="logger">Structured log of the outcome</param>

using Klacks.Api.Application.Commands.Associations;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Associations;

public class SetGroupLocationCommandHandler : BaseHandler, IRequestHandler<SetGroupLocationCommand, bool>
{
    private readonly IGroupRepository _groupRepository;

    public SetGroupLocationCommandHandler(
        IGroupRepository groupRepository,
        ILogger<SetGroupLocationCommandHandler> logger)
        : base(logger)
    {
        _groupRepository = groupRepository;
    }

    public async Task<bool> Handle(SetGroupLocationCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var updated = await _groupRepository.SetCoordinatesAsync(
                command.Id, command.Latitude, command.Longitude, cancellationToken);

            if (!updated)
            {
                throw new KeyNotFoundException($"Group with ID {command.Id} not found");
            }

            return true;
        },
        "setting a group location",
        new { GroupId = command.Id });
    }
}
