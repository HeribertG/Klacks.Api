// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Column length of the client's first name and name: the StringLength of Client.FirstName and Client.Name and the limit
/// validators check before a value reaches the database.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ClientFieldLimits
{
    public const int NameMaxLength = 100;
}
