// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.ClientShiftPreferences;

/// <summary>
/// Upserts a SINGLE client/shift preference (Preferred or Blacklist) without touching the client's
/// other preferences. Deliberately not the bulk SaveClientShiftPreferencesCommand, which replaces all
/// of a client's preferences. Scoped to real preferences (AnalyseToken == null). Returns the row id.
/// </summary>
/// <param name="ClientId">Employee the preference belongs to</param>
/// <param name="ShiftId">Shift the preference is about</param>
/// <param name="PreferenceType">Preferred or Blacklist</param>
public record SetShiftPreferenceCommand(
    Guid ClientId,
    Guid ShiftId,
    ShiftPreferenceType PreferenceType) : IRequest<Guid>;
