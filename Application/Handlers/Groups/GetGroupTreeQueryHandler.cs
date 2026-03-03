// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups
{
    public class GetGroupTreeQueryHandler : BaseHandler, IRequestHandler<GetGroupTreeQuery, GroupTreeResource>
    {
        private readonly IGroupRepository _groupRepository;
        private readonly IGroupItemRepository _groupItemRepository;
        private readonly GroupMapper _groupMapper;

        public GetGroupTreeQueryHandler(
            IGroupRepository groupRepository,
            IGroupItemRepository groupItemRepository,
            GroupMapper groupMapper,
            ILogger<GetGroupTreeQueryHandler> logger)
            : base(logger)
        {
            _groupRepository = groupRepository;
            _groupItemRepository = groupItemRepository;
            _groupMapper = groupMapper;
        }

        public async Task<GroupTreeResource> Handle(GetGroupTreeQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteAsync(async () =>
            {
                var flatNodes = await _groupRepository.GetTree(request.RootId);
                var nodeDict = flatNodes.ToDictionary(g => g.Id, g => _groupMapper.ToGroupResource(g));
                var rootNodes = new List<GroupResource>();

                var shiftCounts = await _groupItemRepository.GetShiftCountsPerGroupAsync(cancellationToken);
                var customerCounts = await _groupItemRepository.GetCustomerCountsPerGroupAsync(cancellationToken);

                foreach (var node in nodeDict.Values)
                {
                    if (shiftCounts.TryGetValue(node.Id, out var shiftCount))
                    {
                        node.ShiftsCount = shiftCount;
                    }

                    if (customerCounts.TryGetValue(node.Id, out var customerCount))
                    {
                        node.CustomersCount = customerCount;
                    }

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

                foreach (var rootNode in rootNodes)
                {
                    CalculateDepthRecursive(rootNode, 0);
                }

                SortChildrenRecursive(rootNodes);

                return new GroupTreeResource
                {
                    RootId = request.RootId,
                    Nodes = rootNodes
                };
            }, nameof(Handle), new { request.RootId });
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
