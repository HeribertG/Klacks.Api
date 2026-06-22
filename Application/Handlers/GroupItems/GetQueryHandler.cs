// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving a single group item by ID.
/// </summary>
/// <param name="request">The GetQuery containing the group item ID to retrieve.</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.GroupItems;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<GroupItemResource>, GroupItemResource?>
{
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly GroupMapper _groupMapper;

    public GetQueryHandler(IGroupItemRepository groupItemRepository, GroupMapper groupMapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _groupItemRepository = groupItemRepository;
        _groupMapper = groupMapper;
    }

    public async Task<GroupItemResource?> Handle(GetQuery<GroupItemResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var groupItem = await _groupItemRepository.Get(request.Id);
            if (groupItem == null)
            {
                throw new KeyNotFoundException($"GroupItem with ID {request.Id} not found.");
            }
            return _groupMapper.ToGroupItemResource(groupItem);
        }, nameof(Handle), new { request.Id });
    }
}
