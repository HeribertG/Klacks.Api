using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.AssignedGroups;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.AssignedGroups
{
    public class GetListQueryHandler : IRequestHandler<AssignedGroupListQuery, IEnumerable<GroupResource>>
    {
        private readonly IAssignedGroupRepository _assignedGroupRepository;
        private readonly IMapper _mapper;

        public GetListQueryHandler(IAssignedGroupRepository assignedGroupRepository, IMapper mapper)
        {
            _assignedGroupRepository = assignedGroupRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<GroupResource>> Handle(AssignedGroupListQuery request, CancellationToken cancellationToken)
        {
            var groups = await _assignedGroupRepository.Assigned(request.Id);
            return _mapper.Map<IEnumerable<GroupResource>>(groups);
        }
    }
}
