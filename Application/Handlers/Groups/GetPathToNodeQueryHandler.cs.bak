using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// CQRS Handler for getting path to a group node
/// Refactored to use Application Service following Clean Architecture
/// </summary>
public class GetPathToNodeQueryHandler : IRequestHandler<GetPathToNodeQuery, List<GroupResource>>
{
    private readonly GroupApplicationService _groupApplicationService;

    public GetPathToNodeQueryHandler(GroupApplicationService groupApplicationService)
    {
        _groupApplicationService = groupApplicationService;
    }

    public async Task<List<GroupResource>> Handle(GetPathToNodeQuery request, CancellationToken cancellationToken)
    {
        // Clean Architecture: Delegate to Application Service
        return await _groupApplicationService.GetPathToNodeAsync(request.NodeId, cancellationToken);
    }
}
