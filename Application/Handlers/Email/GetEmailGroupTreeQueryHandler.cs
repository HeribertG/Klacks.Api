// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Email;

public class GetEmailGroupTreeQueryHandler : BaseHandler, IRequestHandler<GetEmailGroupTreeQuery, List<EmailGroupTreeNode>>
{
    private readonly IGroupHierarchyService _groupHierarchyService;
    private readonly DataBaseContext _context;

    public GetEmailGroupTreeQueryHandler(
        IGroupHierarchyService groupHierarchyService,
        DataBaseContext context,
        ILogger<GetEmailGroupTreeQueryHandler> logger)
        : base(logger)
    {
        _groupHierarchyService = groupHierarchyService;
        _context = context;
    }

    public async Task<List<EmailGroupTreeNode>> Handle(GetEmailGroupTreeQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var receivedAddresses = await _context.ReceivedEmails
                .Where(e => !e.IsDeleted && e.Folder == EmailConstants.ClientAssignedFolder)
                .Select(e => e.FromAddress.ToLower())
                .Distinct()
                .ToListAsync(cancellationToken);

            if (receivedAddresses.Count == 0)
                return [];

            var receivedAddressSet = new HashSet<string>(receivedAddresses, StringComparer.OrdinalIgnoreCase);

            var clientsWithEmails = await _context.Set<Domain.Models.Staffs.Communication>()
                .Where(c => !c.IsDeleted &&
                            (c.Type == CommunicationTypeEnum.PrivateMail || c.Type == CommunicationTypeEnum.OfficeMail) &&
                            !string.IsNullOrEmpty(c.Value))
                .Include(c => c.Client)
                .ThenInclude(cl => cl.GroupItems)
                .Where(c => !c.Client.IsDeleted)
                .ToListAsync(cancellationToken);

            var clientEmailCounts = new Dictionary<Guid, int>();
            var clientGroupMapping = new Dictionary<Guid, HashSet<Guid>>();
            var clientNames = new Dictionary<Guid, string>();

            foreach (var comm in clientsWithEmails)
            {
                if (!receivedAddressSet.Contains(comm.Value)) continue;

                var clientId = comm.ClientId;

                var emailCount = await _context.ReceivedEmails
                    .CountAsync(e => !e.IsDeleted &&
                                     e.Folder == EmailConstants.ClientAssignedFolder &&
                                     e.FromAddress.ToLower() == comm.Value.ToLower(),
                        cancellationToken);

                if (emailCount == 0) continue;

                clientEmailCounts.TryAdd(clientId, 0);
                clientEmailCounts[clientId] += emailCount;

                clientNames.TryAdd(clientId,
                    $"{comm.Client.IdNumber}, {comm.Client.FirstName}, {comm.Client.Name}".TrimEnd(',', ' '));

                clientGroupMapping.TryAdd(clientId, []);
                foreach (var gi in comm.Client.GroupItems.Where(gi => !gi.IsDeleted))
                {
                    clientGroupMapping[clientId].Add(gi.GroupId);
                }
            }

            if (clientEmailCounts.Count == 0)
                return [];

            var allGroups = (await _groupHierarchyService.GetTreeAsync()).ToList();
            var roots = allGroups.Where(g => g.Parent == null).OrderBy(g => g.Lft).ToList();

            var result = new List<EmailGroupTreeNode>();
            foreach (var root in roots)
            {
                var node = BuildGroupNode(root, allGroups, clientEmailCounts, clientGroupMapping, clientNames);
                if (node != null)
                    result.Add(node);
            }

            return result;
        }, nameof(GetEmailGroupTreeQuery));
    }

    private static EmailGroupTreeNode? BuildGroupNode(
        Group group,
        List<Group> allGroups,
        Dictionary<Guid, int> clientEmailCounts,
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
            var childNode = BuildGroupNode(childGroup, allGroups, clientEmailCounts, clientGroupMapping, clientNames);
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
            Children = children
        };
    }
}
