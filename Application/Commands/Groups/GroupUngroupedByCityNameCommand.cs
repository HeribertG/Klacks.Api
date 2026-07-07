// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Groups;

/// <summary>
/// Assigns employees who currently belong to no group to the group whose name exactly matches their
/// address city (case-insensitive). With Apply=false it only previews the assignments; with Apply=true
/// it persists the memberships and re-reads them for verification.
/// </summary>
/// <param name="Count">Optional maximum number of employees to assign; null means all matches.</param>
/// <param name="ValidFrom">Start date of the new memberships (the plannability boundary); null defaults to today.</param>
/// <param name="Apply">False for a dry-run preview, true to persist the memberships.</param>
/// <param name="UserName">Name of the acting user, stored on the created memberships.</param>
public record GroupUngroupedByCityNameCommand(
    int? Count,
    DateTime? ValidFrom,
    bool Apply,
    string UserName) : IRequest<GroupUngroupedByCityNameResult>;
