// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Domain.Services.Groups;

/// <summary>
/// Façade implementation that provides unified access to all group-related services.
/// This follows the Façade pattern to reduce coupling and simplify dependency management.
/// </summary>
public class GroupServiceFacade : IGroupServiceFacade
{
    public IGroupVisibilityService VisibilityService { get; }

    public IGroupTreeService TreeService { get; }

    public IGroupHierarchyService HierarchyService { get; }

    public IGroupSearchService SearchService { get; }

    public IGroupValidityService ValidityService { get; }

    public IGroupMembershipService MembershipService { get; }

    public IGroupIntegrityService IntegrityService { get; }

    public GroupServiceFacade(
        IGroupVisibilityService visibilityService,
        IGroupTreeService treeService,
        IGroupHierarchyService hierarchyService,
        IGroupSearchService searchService,
        IGroupValidityService validityService,
        IGroupMembershipService membershipService,
        IGroupIntegrityService integrityService)
    {
        VisibilityService = visibilityService;
        TreeService = treeService;
        HierarchyService = hierarchyService;
        SearchService = searchService;
        ValidityService = validityService;
        MembershipService = membershipService;
        IntegrityService = integrityService;
    }
}
