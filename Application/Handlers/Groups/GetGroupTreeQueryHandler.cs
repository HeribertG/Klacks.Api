using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetGroupTreeQueryHandler : IRequestHandler<GetGroupTreeQuery, GroupTreeResource>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetGroupTreeQueryHandler> _logger;

        public GetGroupTreeQueryHandler(
            IGroupRepository groupRepository,
            IMapper mapper,
            ILogger<GetGroupTreeQueryHandler> logger)
        {
            _groupRepository = groupRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<GroupTreeResource> Handle(GetGroupTreeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Processing GetGroupTreeQuery with rootId: {request.RootId}");

                var flatNodes = await _groupRepository.GetTree(request.RootId);
                var nodeDict = flatNodes.ToDictionary(g => g.Id, g => _mapper.Map<GroupResource>(g));
                var rootNodes = new List<GroupResource>();
                
                // Build tree structure from flat nodes
                foreach (var node in nodeDict.Values)
                {
                    if (node.Parent == null || !nodeDict.ContainsKey(node.Parent.Value))
                    {
                        rootNodes.Add(node);
                    }
                    else
                    {
                        var parent = nodeDict[node.Parent.Value];
                        parent.Children.Add(node);
                    }
                }
                
                // Calculate depths AFTER tree structure is built
                foreach (var rootNode in rootNodes)
                {
                    CalculateDepthRecursive(rootNode, 0);
                }
                
                SortChildrenRecursive(rootNodes);
                
                var result = new GroupTreeResource
                {
                    RootId = request.RootId,
                    Nodes = rootNodes
                };

                _logger.LogInformation($"Retrieved tree with {result.Nodes.Count} root nodes");

                return result;
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Group tree with root {request.RootId} not found");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing GetGroupTreeQuery with rootId: {request.RootId}");
                throw;
            }
        }
        
        private void CalculateDepthRecursive(GroupResource node, int depth)
        {
            node.Depth = depth;
            foreach (var child in node.Children)
            {
                CalculateDepthRecursive(child, depth + 1);
            }
        }
        
        private void SortChildrenRecursive(List<GroupResource> nodes)
        {
            var sortedNodes = nodes.OrderBy(n => n.Lft).ToList();
            nodes.Clear();
            nodes.AddRange(sortedNodes);
            
            foreach (var node in nodes)
            {
                if (node.Children.Any())
                {
                    SortChildrenRecursive(node.Children);
                }
            }
        }
    }
}