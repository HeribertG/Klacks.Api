// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// One group member's hours balance for the requested period.
/// </summary>
/// <param name="ClientId">UUID of the employee (client).</param>
/// <param name="FirstName">First name of the employee.</param>
/// <param name="LastName">Last name of the employee.</param>
/// <param name="ActualHours">Scheduled (actual) hours in the period.</param>
/// <param name="Surcharges">Surcharge hours in the period.</param>
/// <param name="TargetHours">Contract's guaranteed (target) hours for the period.</param>
/// <param name="Balance">ActualHours minus TargetHours; positive means overtime, negative means under-target.</param>
public record GroupMemberHoursBalance(
    Guid ClientId,
    string FirstName,
    string LastName,
    decimal ActualHours,
    decimal Surcharges,
    decimal TargetHours,
    decimal Balance);
