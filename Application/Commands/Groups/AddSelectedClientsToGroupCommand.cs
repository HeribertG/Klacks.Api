// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Groups;

/// <summary>
/// Adds the clients the user ticked in the UI list (resolved from the page context selection) to an
/// already resolved group. With Apply=false it only previews who would be added; with Apply=true it
/// persists the new memberships and re-reads them to confirm.
/// </summary>
/// <param name="GroupId">Resolved id of the target group.</param>
/// <param name="GroupName">Resolved name of the target group (for the result message).</param>
/// <param name="SelectedClientIds">Ids of the clients the user selected in the list.</param>
/// <param name="ValidFrom">Start date of the new memberships (the plannability boundary); null defaults to now.</param>
/// <param name="Apply">False for a dry-run preview, true to persist the memberships.</param>
/// <param name="UserName">Name of the acting user, stored on the created memberships.</param>
public record AddSelectedClientsToGroupCommand(
    Guid GroupId,
    string GroupName,
    IReadOnlyList<Guid> SelectedClientIds,
    DateTime? ValidFrom,
    bool Apply,
    string UserName) : IRequest<AddSelectedClientsToGroupResult>;
