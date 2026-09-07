// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Decides what the current caller may see of a soft-deleted Work: Admin (any delete, any time), Owner
/// (the caller deleted it himself), or Hidden - a foreign delete the endpoint must answer exactly like
/// "not found", so no validation message can reveal that the Work exists.
/// </summary>
public interface IWorkRestoreAuthorizer
{
    /// <param name="work">The soft-deleted Work with its CurrentUserDeleted stamp</param>
    WorkRestoreAccess Resolve(Work work);
}
