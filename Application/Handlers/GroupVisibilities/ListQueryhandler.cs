using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.GroupVisibilities;

public class ListQueryhandler : IRequestHandler<ListQuery<GroupVisibilityResource>, IEnumerable<GroupVisibilityResource>>
{
    private readonly GroupVisibilityApplicationService _groupVisibilityApplicationService;

    public ListQueryhandler(GroupVisibilityApplicationService groupVisibilityApplicationService)
    {
        _groupVisibilityApplicationService = groupVisibilityApplicationService;
    }

    public async Task<IEnumerable<GroupVisibilityResource>> Handle(ListQuery<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        return await _groupVisibilityApplicationService.GetAllGroupVisibilitiesAsync(cancellationToken);
    }
}
