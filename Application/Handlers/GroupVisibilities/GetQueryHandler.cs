// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<GroupVisibilityResource>, GroupVisibilityResource>
{
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly GroupMapper _groupMapper;

    public GetQueryHandler(IGroupVisibilityRepository groupVisibilityRepository, GroupMapper groupMapper, ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _groupVisibilityRepository = groupVisibilityRepository;
        _groupMapper = groupMapper;
    }

    public async Task<GroupVisibilityResource> Handle(GetQuery<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var groupVisibility = await _groupVisibilityRepository.Get(request.Id);

            if (groupVisibility == null)
            {
                throw new KeyNotFoundException($"Group visibility with ID {request.Id} not found");
            }

            return _groupMapper.ToGroupVisibilityResource(groupVisibility);
        }, nameof(Handle), new { request.Id });
    }
}
