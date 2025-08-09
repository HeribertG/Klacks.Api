using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Staffs;
using Klacks.Api.Application.Queries.AssignedGroups;
using MediatR;

namespace Klacks.Api.Handlers.AssignedGroups
{
    public class GetListQueryHandler : IRequestHandler<AssignedGroupListQuery, IEnumerable<AssignedGroup>>
    {
        private readonly IMapper mapper;
        private readonly IAssignedGroupRepository repository;

        public GetListQueryHandler(IMapper mapper, IAssignedGroupRepository repository)
        {
            this.mapper = mapper;
            this.repository = repository;
        }

        public async Task<IEnumerable<AssignedGroup>> Handle(AssignedGroupListQuery request, CancellationToken cancellationToken)
        {
            var list = await repository.Assigned(request.Id);

            return mapper.Map<IEnumerable<AssignedGroup>, IEnumerable<AssignedGroup>>((IEnumerable<AssignedGroup>)list);
        }
    }
}
