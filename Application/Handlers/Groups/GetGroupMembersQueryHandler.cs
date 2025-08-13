using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups;

public class GetGroupMembersQueryHandler : IRequestHandler<GetGroupMembersQuery, List<GroupItemResource>>
{
    private readonly GroupApplicationService _groupApplicationService;

    public GetGroupMembersQueryHandler(GroupApplicationService groupApplicationService)
    {
        _groupApplicationService = groupApplicationService;
    }

    public async Task<List<GroupItemResource>> Handle(GetGroupMembersQuery request, CancellationToken cancellationToken)
    {
        return await _groupApplicationService.GetGroupMembersAsync(request.GroupId, cancellationToken);
    }
}