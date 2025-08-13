using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Application.Queries.Groups;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetRootsQueryHandler : IRequestHandler<GetRootsQuery, IEnumerable<GroupResource>>
{
    private readonly GroupApplicationService _groupApplicationService;

    public GetRootsQueryHandler(GroupApplicationService groupApplicationService)
    {
        _groupApplicationService = groupApplicationService;
    }
    
    public async Task<IEnumerable<GroupResource>> Handle(GetRootsQuery request, CancellationToken cancellationToken)
    {
        return await _groupApplicationService.GetRootGroupsAsync(cancellationToken);
    }
}
