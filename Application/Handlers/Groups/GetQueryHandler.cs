// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<GroupResource>, GroupResource>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly GroupMapper _groupMapper;

        public GetQueryHandler(IGroupRepository groupRepository, GroupMapper groupMapper, ILogger<GetQueryHandler> logger)
            : base(logger)
        {
            _groupRepository = groupRepository;
            _groupMapper = groupMapper;
        }

        public async Task<GroupResource> Handle(GetQuery<GroupResource> request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var group = await _groupRepository.Get(request.Id);

                if (group == null)
                {
                    throw new KeyNotFoundException($"Group with ID {request.Id} not found");
                }

                return _groupMapper.ToGroupResource(group);
            }, nameof(Handle), new { request.Id });
        }
    }
}
