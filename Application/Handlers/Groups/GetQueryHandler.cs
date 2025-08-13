using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetQueryHandler : IRequestHandler<GetQuery<GroupResource>, GroupResource?>
    {
        private readonly GroupApplicationService _groupApplicationService;

        public GetQueryHandler(GroupApplicationService groupApplicationService)
        {
            _groupApplicationService = groupApplicationService;
        }

        public async Task<GroupResource?> Handle(GetQuery<GroupResource> request, CancellationToken cancellationToken)
        {
            return await _groupApplicationService.GetGroupByIdAsync(request.Id, cancellationToken);
        }
    }
}