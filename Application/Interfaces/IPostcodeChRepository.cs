// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository for Swiss postal code lookups by zip or city name.
/// </summary>

using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.Interfaces;

public interface IPostcodeChRepository
{
    Task<List<PostcodeCH>> GetAllAsync();
    Task<List<PostcodeCH>> GetByZipAsync(int zip);
    Task<List<PostcodeCH>> GetByCityAsync(string city);
}
