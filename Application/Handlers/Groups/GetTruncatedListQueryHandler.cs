using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    /// <summary>
    /// CQRS Handler that uses GroupApplicationService instead of direct Repository access
    /// Follows Clean Architecture - Handler orchestrates, Application Service contains business logic
    /// </summary>
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedGroupResource>
    {
        private readonly GroupApplicationService _groupApplicationService;

        public GetTruncatedListQueryHandler(GroupApplicationService groupApplicationService)
        {
            _groupApplicationService = groupApplicationService;
        }

        public async Task<TruncatedGroupResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
        {
            // Clean Architecture: Handler delegates to Application Service
            // Application Service handles DTO→Domain→DTO mapping
            return await _groupApplicationService.SearchGroupsAsync(request.Filter, cancellationToken);
        }
    }
}
