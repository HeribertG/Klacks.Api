// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Staffs;

/// <summary>
/// One client of the CSV client export: the eight columns the export writes, with missing texts read as empty strings.
/// </summary>
/// <param name="IdNumber">Client number</param>
/// <param name="Company">Company name, empty when none</param>
/// <param name="FirstName">First name, empty when none</param>
/// <param name="Name">Last name, empty when none</param>
/// <param name="Birthdate">Date of birth, null when unknown</param>
/// <param name="Gender">Gender</param>
/// <param name="Type">Entity type (employee, external employee, customer)</param>
/// <param name="LegalEntity">Whether the client is a legal entity</param>
public sealed record ClientExportRow(
    int IdNumber,
    string Company,
    string FirstName,
    string Name,
    DateTime? Birthdate,
    GenderEnum Gender,
    EntityTypeEnum Type,
    bool LegalEntity);
