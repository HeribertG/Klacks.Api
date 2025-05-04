using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries;
using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Handlers.Groups
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