// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Domain.Interfaces.Associations;

/// <summary>
/// Façade interface that provides access to all group-related services.
/// This reduces the number of dependencies in GroupRepository from 8 to 1.
/// </summary>
public interface IGroupServiceFacade
{
    IGroupVisibilityService VisibilityService { get; }

    IGroupTreeService TreeService { get; }

    IGroupHierarchyService HierarchyService { get; }

    IGroupSearchService SearchService { get; }

    IGroupValidityService ValidityService { get; }

    IGroupMembershipService MembershipService { get; }

    IGroupIntegrityService IntegrityService { get; }
}
