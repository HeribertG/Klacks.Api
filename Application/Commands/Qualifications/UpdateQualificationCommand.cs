// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Qualifications;

/// <summary>
/// Updates an existing qualification's name, description, emoji and time-limited flag.
/// </summary>
/// <param name="Id">Id of the qualification to update</param>
/// <param name="Name">New multilingual display name</param>
/// <param name="Description">Optional multilingual free-text description</param>
/// <param name="Emoji">Emoji character(s) representing this qualification</param>
/// <param name="IsTimeLimited">Whether this qualification has an expiry date on client assignments</param>
public record UpdateQualificationCommand(
    Guid Id,
    MultiLanguage Name,
    MultiLanguage? Description,
    string? Emoji,
    bool IsTimeLimited) : IRequest<Qualification>;
