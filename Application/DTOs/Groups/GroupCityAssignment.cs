// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// A single planned or applied assignment of an ungrouped employee to the group whose name
/// exactly matches the employee's address city.
/// </summary>
/// <param name="ClientId">Id of the ungrouped employee.</param>
/// <param name="ClientName">Display name of the employee (for the preview message).</param>
/// <param name="City">The employee's address city that produced the match.</param>
/// <param name="GroupId">Id of the target group whose name equals the city.</param>
/// <param name="GroupName">Name of the target group.</param>
public record GroupCityAssignment(
    Guid ClientId,
    string ClientName,
    string City,
    Guid GroupId,
    string GroupName);
