using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using Klacks.Api.Resources.Staffs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Handlers.Groups;

/// <summary>
/// Handler für die GetGroupTreeQuery
/// </summary>
public class GetGroupTreeQueryHandler : IRequestHandler<GetGroupTreeQuery, GroupTreeResource>
{
    private readonly IGroupRepository _repository;
    private readonly DataBaseContext _context;

    public GetGroupTreeQueryHandler(IGroupRepository repository, DataBaseContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<GroupTreeResource> Handle(GetGroupTreeQuery request, CancellationToken cancellationToken)
    {
        var nodes = await _repository.GetTree(request.RootId);
        var nodesList = nodes.OrderBy(n => n.Lft).ToList();

        if (!nodesList.Any())
        {
            return new GroupTreeResource
            {
                RootId = request.RootId,
                Nodes = new List<GroupTreeNodeResource>()
            };
        }

        var result = new GroupTreeResource
        {
            RootId = request.RootId ?? nodesList.FirstOrDefault(n => n.Parent == null)?.Id,
            Nodes = new List<GroupTreeNodeResource>()
        };

        // Eine Hierarchie aus flachen Nodes bauen
        foreach (var node in nodesList)
        {
            // Tiefe berechnen
            var ancestorCount = nodesList.Count(n => n.Lft < node.Lft && n.rgt > node.rgt && n.Root == node.Root);
            var clientsCount = await _context.GroupItem.CountAsync(gi => gi.GroupId == node.Id, cancellationToken);

            result.Nodes.Add(new GroupTreeNodeResource
            {
                Id = node.Id,
                Name = node.Name,
                Description = node.Description,
                ValidFrom = node.ValidFrom,
                ValidUntil = node.ValidUntil,
                ParentId = node.Parent,
                Root = node.Root,
                Lft = node.Lft,
                Rgt = node.rgt,
                Depth = ancestorCount,
                ClientsCount = clientsCount,
                CreateTime = node.CreateTime,
                UpdateTime = node.UpdateTime
            });
        }

        return result;
    }
}

/// <summary>
/// Handler für die GetGroupNodeDetailsQuery
/// </summary>
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
            // Weitere Eigenschaften hier hinzufügen
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
            ClientsCount = clientsResource.Count,
            CreateTime = group.CreateTime,
            UpdateTime = group.UpdateTime,
            CurrentUserCreated = group.CurrentUserCreated,
            CurrentUserUpdated = group.CurrentUserUpdated
        };
    }
}

/// <summary>
/// Handler für die GetPathToNodeQuery
/// </summary>
public class GetPathToNodeQueryHandler : IRequestHandler<GetPathToNodeQuery, List<GroupTreeNodeResource>>
{
    private readonly IGroupRepository _repository;
    private readonly DataBaseContext _context;

    public GetPathToNodeQueryHandler(IGroupRepository repository, DataBaseContext context)
    {
        _repository = repository;
        _context = context;
    }

    public async Task<List<GroupTreeNodeResource>> Handle(GetPathToNodeQuery request, CancellationToken cancellationToken)
    {
        var pathNodes = await _repository.GetPath(request.NodeId);

        var result = new List<GroupTreeNodeResource>();
        var depth = 0;

        foreach (var node in pathNodes.OrderBy(n => n.Lft))
        {
            var clientsCount = await _context.GroupItem.CountAsync(gi => gi.GroupId == node.Id, cancellationToken);

            result.Add(new GroupTreeNodeResource
            {
                Id = node.Id,
                Name = node.Name,
                Description = node.Description,
                ValidFrom = node.ValidFrom,
                ValidUntil = node.ValidUntil,
                ParentId = node.Parent,
                Root = node.Root,
                Lft = node.Lft,
                Rgt = node.rgt,
                Depth = depth++,
                ClientsCount = clientsCount,
                CreateTime = node.CreateTime,
                UpdateTime = node.UpdateTime
            });
        }

        return result;
    }
}