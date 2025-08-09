using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Application.Queries;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetQueryHandler(IMapper mapper, IGroupRepository repository)
      : IRequestHandler<GetQuery<GroupResource>, GroupResource?>
    {
        public async Task<GroupResource?> Handle(GetQuery<GroupResource> request, CancellationToken cancellationToken)
        {
            var group = await repository.Get(request.Id);
            if (group != null)
            {
                return mapper.Map<Models.Associations.Group, GroupResource>(group);
            }

            return null;
        }
    }
}