// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The name data of one person or organisation as the pseudonym dictionary reads it from the database.
/// </summary>
/// <param name="Kind">Employee (also external and stand-in), Customer or User</param>
/// <param name="EntityKey">The database id, the stable key of the pseudonym</param>
/// <param name="Gender">The gender for the model's legend, null when unknown (users)</param>
/// <param name="IdNumber">The personnel number, null for users</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public sealed record PseudonymPerson(
    PseudonymKind Kind,
    string EntityKey,
    GenderEnum? Gender,
    string? FirstName,
    string? LastName,
    string? SecondName,
    string? MaidenName,
    string? Company,
    bool LegalEntity,
    int? IdNumber);
