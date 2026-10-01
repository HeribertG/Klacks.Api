// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Everything a row transformation needs besides the row itself, loaded once per preview or commit:
/// company "today", the effective policy values, known contracts and groups, the resolved countries of
/// every distinct country cell, the user's per-row choices keyed by row index, and the existing
/// employees that could be duplicates.
/// </summary>

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportLookup
{
    public DateTime Today { get; init; }

    public DateTime PolicyEntryDate { get; init; }

    public Countries? DefaultCountry { get; init; }

    public ClientImportNamedEntity? PolicyContract { get; init; }

    public ClientImportNamedEntity? PolicyGroup { get; init; }

    public IReadOnlyList<ClientImportNamedEntity> Contracts { get; init; } = [];

    public IReadOnlyList<ClientImportNamedEntity> Groups { get; init; } = [];

    public IReadOnlyDictionary<string, Countries?> CountriesByValue { get; init; } = new Dictionary<string, Countries?>();

    public IReadOnlyDictionary<int, ClientImportRowOverride> RowOverrides { get; init; } = new Dictionary<int, ClientImportRowOverride>();

    public IReadOnlyList<ClientImportExistingClient> ExistingClients { get; set; } = [];
}
