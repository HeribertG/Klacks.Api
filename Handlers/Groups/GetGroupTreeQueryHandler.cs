using AutoMapper;
using Klacks.Api.Interfaces;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using MediatR;

namespace Klacks.Api.Handlers.Groups
{
    public class GetGroupTreeQueryHandler : IRequestHandler<GetGroupTreeQuery, GroupTreeResource>
    {
        private readonly IGroupRepository repository;
        private readonly IMapper mapper;
        private readonly ILogger<GetGroupTreeQueryHandler> logger;

        public GetGroupTreeQueryHandler(
            IGroupRepository repository,
            IMapper mapper,
            ILogger<GetGroupTreeQueryHandler> logger)
        {
            this.repository = repository;
            this.mapper = mapper;
            this.logger = logger;
        }

        public async Task<GroupTreeResource> Handle(GetGroupTreeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                logger.LogInformation($"Processing GetGroupTreeQuery with rootId: {request.RootId}");

                var treeNodes = await repository.GetTree(request.RootId);

                var result = new GroupTreeResource
                {
                    RootId = request.RootId,
                    Nodes = new List<GroupResource>()
                };

                var allNodes = treeNodes.ToList();

                if (allNodes.Any())
                {
                    logger.LogInformation($"Found {allNodes.Count} nodes in tree");

                    await BuildTreeHierarchy(allNodes, result);
                }
                else
                {
                    logger.LogInformation("No nodes found in tree");
                }

                return result;
            }
            catch (KeyNotFoundException ex)
            {
                logger.LogWarning(ex, $"Group tree with root {request.RootId} not found");
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing GetGroupTreeQuery with rootId: {request.RootId}");
                throw;
            }
        }

        private async Task BuildTreeHierarchy(List<Models.Associations.Group> allNodes, GroupTreeResource result)
        {
            try
            {
                // 1. Erzeuge eine Lookup-Tabelle für Knoten nach ID
                var nodesById = allNodes.ToDictionary(n => n.Id);

                // 2. Gruppiere Knoten nach Parent-ID für schnellen Zugriff auf Kinder
                var childrenByParentId = allNodes
                    .Where(n => n.Parent.HasValue)
                    .GroupBy(n => n.Parent!.Value)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // 3. Finde Root-Knoten basierend auf dem angegebenen Root-ID oder als Knoten ohne Eltern
                var rootNodes = result.RootId.HasValue
                    ? allNodes.Where(n => n.Parent == null || n.Id == result.RootId.Value).ToList()
                    : allNodes.Where(n => n.Parent == null).ToList();

                logger.LogInformation($"Found {rootNodes.Count} root nodes");

                // 4. Rekursive Funktion zum Aufbau des Baums
                foreach (var rootNode in rootNodes)
                {
                    var nodeResource = await CreateNodeResourceWithChildren(rootNode, childrenByParentId);
                    result.Nodes.Add(nodeResource);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error building tree hierarchy");
                throw;
            }
        }

        private async Task<GroupResource> CreateNodeResourceWithChildren(
            Models.Associations.Group node,
            Dictionary<Guid, List<Models.Associations.Group>> childrenByParentId)
        {
            // Erstelle Ressource für den aktuellen Knoten
            var nodeResource = mapper.Map<GroupResource>(node);

            // Berechne Tiefe
            var depth = await repository.GetNodeDepth(node.Id);
            nodeResource.Depth = depth;

            // Initialisiere Children-Liste
            nodeResource.Children = new List<GroupResource>();

            // Wenn keine Kinder, gebe den Knoten direkt zurück
            if (!childrenByParentId.TryGetValue(node.Id, out var childNodes))
            {
                return nodeResource;
            }

            // Rekursiver Aufruf für alle Kinder
            foreach (var childNode in childNodes)
            {
                var childResource = await CreateNodeResourceWithChildren(childNode, childrenByParentId);
                nodeResource.Children.Add(childResource);
            }

            return nodeResource;
        }
    }
}