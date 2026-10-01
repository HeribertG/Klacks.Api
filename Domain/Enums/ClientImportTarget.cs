// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ClientImportTarget>))]
public enum ClientImportTarget
{
    Ignore,
    FullName,
    FirstName,
    LastName,
    Title,
    Salutation,
    Gender,
    Birthdate,
    Street,
    HouseNumber,
    AddressLine2,
    Zip,
    City,
    ZipCity,
    State,
    Country,
    Email,
    Phone,
    Mobile,
    EntryDate,
    ExitDate,
    Contract,
    Group,
    PersonnelNumber,
    Note,
}
