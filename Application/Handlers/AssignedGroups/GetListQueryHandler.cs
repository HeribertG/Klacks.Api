using Klacks.Api.Application.Queries.AssignedGroups;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.AssignedGroups
{
    public class GetListQueryHandler : IRequestHandler<AssignedGroupListQuery, IEnumerable<GroupResource>>
    {
        private readonly AssignedGroupApplicationService _assignedGroupApplicationService;

        public GetListQueryHandler(AssignedGroupApplicationService assignedGroupApplicationService)
        {
            _assignedGroupApplicationService = assignedGroupApplicationService;
        }

        public async Task<IEnumerable<GroupResource>> Handle(AssignedGroupListQuery request, CancellationToken cancellationToken)
        {
            return await _assignedGroupApplicationService.GetAssignedGroupsAsync(request.Id, cancellationToken);
        }
    }
}
