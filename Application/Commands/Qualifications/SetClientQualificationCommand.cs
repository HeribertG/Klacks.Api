// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Qualifications;

/// <summary>
/// Upserts a client's qualification: if an active row for (ClientId, QualificationId) exists its
/// level / validity / note are updated, otherwise a new row is created. Upsert is required because
/// the partial unique index forbids two active rows with the same key. Returns the row id.
/// </summary>
/// <param name="ClientId">Employee who holds the qualification</param>
/// <param name="QualificationId">The qualification being set</param>
/// <param name="Level">Proficiency level (Low..Expert)</param>
/// <param name="ValidFrom">Optional start of validity (null = always valid up to ValidUntil)</param>
/// <param name="ValidUntil">Optional end of validity (null = no expiry)</param>
/// <param name="Note">Optional free-text note</param>
public record SetClientQualificationCommand(
    Guid ClientId,
    Guid QualificationId,
    QualificationLevel Level,
    DateOnly? ValidFrom,
    DateOnly? ValidUntil,
    string? Note) : IRequest<Guid>;
