using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Handlers.Groups;

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

        // Erstelle ein Dictionary zum Nachschlagen von Knoten anhand ihrer ID
        Dictionary<Guid, GroupTreeNodeResource> nodesDict = new Dictionary<Guid, GroupTreeNodeResource>();

        // Erstelle Knoten-Ressourcen für jeden Knoten
        foreach (var node in nodesList)
        {
            // Statt count query die GroupItems Collection nutzen, die wir bereits geladen haben
            int clientsCount = node.GroupItems?.Count ?? 0;

            // Berechne die Tiefe anhand der Nested Set-Struktur
            var depth = nodesList.Count(n => n.Lft < node.Lft && n.rgt > node.rgt && n.Root == node.Root);

            // Erstelle ClientResource-Objekte aus den GroupItems
            var clientsResource = new List<ClientResource>();
            if (node.GroupItems != null)
            {
                clientsResource = node.GroupItems.Select(gi => new ClientResource
                {
                    Id = gi.ClientId,
                    Name = gi.Client?.Name ?? string.Empty,
                    // Weitere Eigenschaften hier hinzufügen, je nach Bedarf
                }).ToList();
            }

            var treeNode = new GroupTreeNodeResource
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
                Depth = depth,
                // Setze die Clients-Sammlung aus den GroupItems
                Clients = clientsResource,
                ClientsCount = clientsCount,
                CreateTime = node.CreateTime,
                UpdateTime = node.UpdateTime,
                CurrentUserCreated = node.CurrentUserCreated,
                CurrentUserUpdated = node.CurrentUserUpdated,
                Children = new List<GroupTreeNodeResource>()
            };

            nodesDict[node.Id] = treeNode;
        }

        // Baue die Hierarchie auf, indem Knoten zu ihren Eltern hinzugefügt werden
        foreach (var node in nodesDict.Values)
        {
            if (node.ParentId.HasValue && nodesDict.ContainsKey(node.ParentId.Value))
            {
                var parent = nodesDict[node.ParentId.Value];
                parent.Children.Add(node);
            }
        }

        // Die Hauptliste enthält nur die Root-Knoten
        var rootNodes = nodesDict.Values
            .Where(n => !n.ParentId.HasValue || !nodesDict.ContainsKey(n.ParentId.Value))
            .ToList();

        // Sortiere alle Bäume nach Root und dann nach Lft
        rootNodes = rootNodes
            .OrderBy(n => n.Root)
            .ThenBy(n => n.Lft)
            .ToList();

        return new GroupTreeResource
        {
            RootId = request.RootId ?? rootNodes.FirstOrDefault()?.Id,
            Nodes = rootNodes
        };
    }
}