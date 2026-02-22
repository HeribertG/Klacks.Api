// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AssignedGroups;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<AssignedGroupResource>, AssignedGroupResource>
{
    private readonly IAssignedGroupRepository _assignedGroupRepository;
    private readonly GroupMapper _groupMapper;

    public GetQueryHandler(IAssignedGroupRepository assignedGroupRepository, GroupMapper groupMapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _assignedGroupRepository = assignedGroupRepository;
        _groupMapper = groupMapper;
    }

    public async Task<AssignedGroupResource> Handle(GetQuery<AssignedGroupResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var assignedGroup = await _assignedGroupRepository.Get(request.Id);

            if (assignedGroup == null)
            {
                throw new KeyNotFoundException($"Assigned group with ID {request.Id} not found");
            }

            return _groupMapper.ToAssignedGroupResource(assignedGroup);
        }, nameof(Handle), new { request.Id });
    }
}
