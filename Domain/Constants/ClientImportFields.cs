// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Field names an import issue points at; they equal the camelCase property names of the preview record
/// so the UI can place an issue in its cell.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ClientImportFields
{
    public const string FirstName = "firstName";
    public const string LastName = "lastName";
    public const string Title = "title";
    public const string Gender = "gender";
    public const string Birthdate = "birthdate";
    public const string Street = "street";
    public const string Zip = "zip";
    public const string City = "city";
    public const string Country = "country";
    public const string Email = "email";
    public const string Phone = "phone";
    public const string Mobile = "mobile";
    public const string EntryDate = "entryDate";
    public const string ExitDate = "exitDate";
    public const string Contract = "contractName";
    public const string Group = "groupName";
}
