using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Handlers.GroupVisibilities;

public class GetQueryHandler(IMapper mapper, IGroupVisibilityRepository repository)
      : IRequestHandler<GetQuery<GroupVisibilityResource>, GroupVisibilityResource?>
{
    public async Task<GroupVisibilityResource?> Handle(GetQuery<GroupVisibilityResource> request, CancellationToken cancellationToken)
    {
        var groupVisibility = await repository.Get(request.Id);
        if (groupVisibility != null)
        {
            return mapper.Map<Models.Associations.GroupVisibility, GroupVisibilityResource>(groupVisibility);
        }

        return null;
    }
}
