using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedGroupResource>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IMapper _mapper;

        public GetTruncatedListQueryHandler(IGroupRepository groupRepository, IMapper mapper)
        {
            _groupRepository = groupRepository;
            _mapper = mapper;
        }

        public async Task<TruncatedGroupResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
        {
            var truncatedResult = await _groupRepository.Truncated(request.Filter);
            
            var truncatedGroupResource = new TruncatedGroupResource
            {
                Groups = _mapper.Map<List<Klacks.Api.Presentation.DTOs.Associations.GroupResource>>(truncatedResult.Groups),
                MaxItems = truncatedResult.MaxItems,
                MaxPages = truncatedResult.MaxPages,
                CurrentPage = truncatedResult.CurrentPage,
                FirstItemOnPage = truncatedResult.FirstItemOnPage
            };
            
            return truncatedGroupResource;
        }
    }
}
