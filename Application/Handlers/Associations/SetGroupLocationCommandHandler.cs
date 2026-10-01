// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Stores a group's coordinates. This needs its own endpoint rather than the generic group PUT:
/// GroupResource carries no latitude or longitude at all, and the write additionally marks the group as
/// geocoded — a flag the caller has no business setting by hand.
/// A group-restricted caller may only locate a visible group; a hidden one is answered exactly like a
/// missing one, before the self-committing write.
/// </summary>
/// <param name="groupRepository">Owns the coordinate write and the geocoded marker</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may write the group</param>
/// <param name="logger">Structured log of the outcome</param>

using Klacks.Api.Application.Commands.Associations;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Associations;

public class SetGroupLocationCommandHandler : BaseHandler, IRequestHandler<SetGroupLocationCommand, bool>
{
    private const string GroupNotFoundMessage = "Group with ID {0} not found";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public SetGroupLocationCommandHandler(
        IGroupRepository groupRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<SetGroupLocationCommandHandler> logger)
        : base(logger)
    {
        _groupRepository = groupRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<bool> Handle(SetGroupLocationCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(command.Id, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, command.Id));
            }

            var updated = await _groupRepository.SetCoordinatesAsync(
                command.Id, command.Latitude, command.Longitude, cancellationToken);

            if (!updated)
            {
                throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, command.Id));
            }

            return true;
        },
        "setting a group location",
        new { GroupId = command.Id });
    }
}
