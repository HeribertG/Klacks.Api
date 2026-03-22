// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for building a group tree with email counters per client and group.
/// </summary>

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Email;

public class GetEmailGroupTreeQueryHandler : BaseHandler, IRequestHandler<GetEmailGroupTreeQuery, List<EmailGroupTreeNode>>
{
    private readonly IGroupHierarchyService _groupHierarchyService;
    private readonly IEmailQueryRepository _emailQueryRepository;

    public GetEmailGroupTreeQueryHandler(
        IGroupHierarchyService groupHierarchyService,
        IEmailQueryRepository emailQueryRepository,
        ILogger<GetEmailGroupTreeQueryHandler> logger)
        : base(logger)
    {
        _groupHierarchyService = groupHierarchyService;
        _emailQueryRepository = emailQueryRepository;
    }

    public async Task<List<EmailGroupTreeNode>> Handle(GetEmailGroupTreeQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var receivedAddresses = await _emailQueryRepository.GetDistinctAssignedEmailAddressesAsync(
                EmailConstants.ClientAssignedFolder, cancellationToken);

            if (receivedAddresses.Count == 0)
                return [];

            var receivedAddressSet = new HashSet<string>(receivedAddresses, StringComparer.OrdinalIgnoreCase);

            var clientsWithEmails = await _emailQueryRepository.GetClientsWithEmailCommunicationsAsync(cancellationToken);

            var clientEmailCounts = new Dictionary<Guid, int>();
            var clientUnreadCounts = new Dictionary<Guid, int>();
            var clientGroupMapping = new Dictionary<Guid, HashSet<Guid>>();
            var clientNames = new Dictionary<Guid, string>();

            foreach (var comm in clientsWithEmails)
            {
                if (!receivedAddressSet.Contains(comm.EmailAddress)) continue;

                var clientId = comm.ClientId;

                var emailCount = await _emailQueryRepository.CountEmailsByAddressAsync(
                    EmailConstants.ClientAssignedFolder, comm.EmailAddress, cancellationToken);

                if (emailCount == 0) continue;

                var unreadCount = await _emailQueryRepository.CountUnreadEmailsByAddressAsync(
                    EmailConstants.ClientAssignedFolder, comm.EmailAddress, cancellationToken);

                clientEmailCounts.TryAdd(clientId, 0);
                clientEmailCounts[clientId] += emailCount;

                clientUnreadCounts.TryAdd(clientId, 0);
                clientUnreadCounts[clientId] += unreadCount;

                clientNames.TryAdd(clientId, comm.ClientDisplayName);

                clientGroupMapping.TryAdd(clientId, []);
                foreach (var groupId in comm.GroupIds)
                {
                    clientGroupMapping[clientId].Add(groupId);
                }
            }

            if (clientEmailCounts.Count == 0)
                return [];

            var allGroups = (await _groupHierarchyService.GetTreeAsync()).ToList();
            var roots = allGroups.Where(g => g.Parent == null).OrderBy(g => g.Lft).ToList();

            var result = new List<EmailGroupTreeNode>();
            foreach (var root in roots)
            {
                var node = BuildGroupNode(root, allGroups, clientEmailCounts, clientUnreadCounts, clientGroupMapping, clientNames);
                if (node != null)
                    result.Add(node);
            }

            var assignedClientIds = new HashSet<Guid>();
            CollectClientIds(result, assignedClientIds);

            var unassignedChildren = new List<EmailGroupTreeNode>();
            foreach (var (clientId, emailCount) in clientEmailCounts)
            {
                if (assignedClientIds.Contains(clientId)) continue;
                unassignedChildren.Add(new EmailGroupTreeNode
                {
                    Id = clientId,
                    Name = clientNames.GetValueOrDefault(clientId, string.Empty),
                    Type = EmailGroupNodeType.Client,
                    EmailCount = emailCount,
                    UnreadCount = clientUnreadCounts.GetValueOrDefault(clientId, 0),
                    Children = []
                });
            }

            if (unassignedChildren.Count > 0)
            {
                result.Add(new EmailGroupTreeNode
                {
                    Id = Guid.Empty,
                    Name = "Unassigned",
                    Type = EmailGroupNodeType.Group,
                    EmailCount = unassignedChildren.Sum(c => c.EmailCount),
                    UnreadCount = 0,
                    Children = unassignedChildren
                });
            }

            return result;
        }, nameof(GetEmailGroupTreeQuery));
    }

    private static void CollectClientIds(List<EmailGroupTreeNode> nodes, HashSet<Guid> clientIds)
    {
        foreach (var node in nodes)
        {
            if (node.Type == EmailGroupNodeType.Client)
                clientIds.Add(node.Id);
            if (node.Children.Count > 0)
                CollectClientIds(node.Children, clientIds);
        }
    }

    private static EmailGroupTreeNode? BuildGroupNode(
        Group group,
        List<Group> allGroups,
        Dictionary<Guid, int> clientEmailCounts,
        Dictionary<Guid, int> clientUnreadCounts,
        Dictionary<Guid, HashSet<Guid>> clientGroupMapping,
        Dictionary<Guid, string> clientNames)
    {
        var children = new List<EmailGroupTreeNode>();

        var childGroups = allGroups
            .Where(g => g.Parent == group.Id)
            .OrderBy(g => g.Lft)
            .ToList();

        foreach (var childGroup in childGroups)
        {
            var childNode = BuildGroupNode(childGroup, allGroups, clientEmailCounts, clientUnreadCounts, clientGroupMapping, clientNames);
            if (childNode != null)
                children.Add(childNode);
        }

        foreach (var (clientId, groupIds) in clientGroupMapping)
        {
            if (!groupIds.Contains(group.Id)) continue;
            if (!clientEmailCounts.TryGetValue(clientId, out var emailCount)) continue;

            children.Add(new EmailGroupTreeNode
            {
                Id = clientId,
                Name = clientNames.GetValueOrDefault(clientId, string.Empty),
                Type = EmailGroupNodeType.Client,
                EmailCount = emailCount,
                UnreadCount = clientUnreadCounts.GetValueOrDefault(clientId, 0),
                Children = []
            });
        }

        if (children.Count == 0) return null;

        var totalEmails = children.Sum(c => c.EmailCount);

        return new EmailGroupTreeNode
        {
            Id = group.Id,
            Name = group.Name,
            Type = EmailGroupNodeType.Group,
            EmailCount = totalEmails,
            UnreadCount = 0,
            Children = children
        };
    }
}
