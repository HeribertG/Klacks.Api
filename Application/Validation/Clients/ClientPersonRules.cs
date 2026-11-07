// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The rules a natural person (employee, not a legal entity) must satisfy, shared by the client POST/PUT
/// validators and the employee import so all three accept exactly the same records: a first name, a
/// last name and one of the person genders Female, Male or Intersexuality. A stand-in needs its display name (Name)
/// only: first name and gender are not required for it.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Clients;

namespace Klacks.Api.Application.Validation.Clients;

public static class ClientPersonRules
{
    public const string FirstNameRequiredMessage = "address.edit-address.address-persona.validation.firstname-required";
    public const string NameRequiredMessage = "address.edit-address.address-persona.validation.name-required";
    public const string GenderRequiredMessage = "address.edit-address.address-persona.validation.gender-required";

    public static bool HasName(string? value) => !string.IsNullOrWhiteSpace(value);

    public static bool RequiresFirstNameAndGender(int clientType) => !ClientTypeRules.IsStandIn((EntityTypeEnum)clientType);

    public static bool IsPersonGender(GenderEnum gender) =>
        gender is GenderEnum.Female or GenderEnum.Male or GenderEnum.Intersexuality;
}
