using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetQueryHandler : IRequestHandler<GetQuery<GroupResource>, GroupResource?>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IMapper _mapper;

        public GetQueryHandler(IGroupRepository groupRepository, IMapper mapper)
        {
            _groupRepository = groupRepository;
            _mapper = mapper;
        }

        public async Task<GroupResource?> Handle(GetQuery<GroupResource> request, CancellationToken cancellationToken)
        {
            var group = await _groupRepository.Get(request.Id);
            return group != null ? _mapper.Map<GroupResource>(group) : null;
        }
    }
}