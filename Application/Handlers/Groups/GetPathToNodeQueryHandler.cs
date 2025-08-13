using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetPathToNodeQueryHandler : IRequestHandler<GetPathToNodeQuery, List<GroupResource>>
{
    private readonly GroupApplicationService _groupApplicationService;

    public GetPathToNodeQueryHandler(GroupApplicationService groupApplicationService)
    {
        _groupApplicationService = groupApplicationService;
    }

    public async Task<List<GroupResource>> Handle(GetPathToNodeQuery request, CancellationToken cancellationToken)
    {
        return await _groupApplicationService.GetPathToNodeAsync(request.NodeId, cancellationToken);
    }
}
