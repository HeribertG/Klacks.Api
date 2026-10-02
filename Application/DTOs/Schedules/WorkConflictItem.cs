// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One schedule conflict that made the backend refuse a work write, as sent in the 409 body.
/// </summary>
/// <param name="Code">Validation key of the conflict, e.g. the missing-qualification key</param>
/// <param name="ClientId">Employee the conflict concerns</param>
/// <param name="Date">Day the conflict concerns</param>
/// <param name="Params">Parameters of the validation key, e.g. qualificationId and minLevel</param>
namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record WorkConflictItem(
    string Code,
    Guid ClientId,
    DateOnly Date,
    IReadOnlyDictionary<string, string> Params);
