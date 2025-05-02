using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using Klacks.Api.Resources.Staffs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Handlers.Groups;

public class GetGroupNodeDetailsQueryHandler : IRequestHandler<GetGroupNodeDetailsQuery, GroupTreeNodeResource>
{
    private readonly IGroupRepository _repository;
    private readonly DataBaseContext _context;

    public GetGroupNodeDetailsQueryHandler(IGroupRepository repository, DataBaseContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<GroupTreeNodeResource> Handle(GetGroupNodeDetailsQuery request, CancellationToken cancellationToken)
    {
        var group = await _context.Group
            .Include(g => g.GroupItems)
            .ThenInclude(gi => gi.Client)
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.Id && !g.IsDeleted, cancellationToken);

        if (group == null)
        {
            throw new KeyNotFoundException($"Gruppe mit ID {request.Id} nicht gefunden");
        }

        var depth = await _repository.GetNodeDepth(request.Id);
        var clientsResource = group.GroupItems.Select(gi => new ClientResource
        {
            Id = gi.ClientId,
            Name = gi.Client?.Name ?? string.Empty,
        }).ToList();

        return new GroupTreeNodeResource
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            ValidFrom = group.ValidFrom,
            ValidUntil = group.ValidUntil,
            ParentId = group.Parent,
            Root = group.Root,
            Lft = group.Lft,
            Rgt = group.rgt,
            Depth = depth,
            Clients = clientsResource,
            ClientsCount = clientsResource.Count(),
            CreateTime = group.CreateTime,
            UpdateTime = group.UpdateTime,
            CurrentUserCreated = group.CurrentUserCreated,
            CurrentUserUpdated = group.CurrentUserUpdated
        };
    }
}