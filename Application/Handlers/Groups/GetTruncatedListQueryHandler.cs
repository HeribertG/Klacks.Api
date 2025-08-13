using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedGroupResource>
    {
        private readonly GroupApplicationService _groupApplicationService;

        public GetTruncatedListQueryHandler(GroupApplicationService groupApplicationService)
        {
            _groupApplicationService = groupApplicationService;
        }

        public async Task<TruncatedGroupResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
        {
            return await _groupApplicationService.SearchGroupsAsync(request.Filter, cancellationToken);
        }
    }
}
