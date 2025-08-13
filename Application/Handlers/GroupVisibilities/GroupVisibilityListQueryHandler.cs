using Klacks.Api.Application.Queries.GroupVisibilities;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class GroupVisibilityListQueryHandler : IRequestHandler<GroupVisibilityListQuery, IEnumerable<GroupVisibilityResource>>
{
    private readonly GroupVisibilityApplicationService _groupVisibilityApplicationService;

    public GroupVisibilityListQueryHandler(GroupVisibilityApplicationService groupVisibilityApplicationService)
    {
        _groupVisibilityApplicationService = groupVisibilityApplicationService;
    }

    public async Task<IEnumerable<GroupVisibilityResource>> Handle(GroupVisibilityListQuery request, CancellationToken cancellationToken)
    {
        return await _groupVisibilityApplicationService.GetGroupVisibilityListAsync(request.Id, cancellationToken);
    }
}
