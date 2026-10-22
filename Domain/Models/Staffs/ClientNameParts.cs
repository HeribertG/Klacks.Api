// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Staffs;

/// <summary>
/// Id and the two name columns of a client, read without any navigation collection.
/// </summary>
/// <param name="Id">Client id</param>
/// <param name="FirstName">First name as stored (may be null or empty)</param>
/// <param name="Name">Last name as stored (may be null or empty)</param>
public sealed record ClientNameParts(Guid Id, string? FirstName, string? Name);
