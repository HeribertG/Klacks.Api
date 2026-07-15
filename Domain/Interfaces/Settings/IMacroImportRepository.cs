// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for the region-setup entity-import path (K20) over
/// <see cref="Klacks.Api.Domain.Models.Settings.Macro"/> rows: lookup by natural import keys for the
/// re-apply reconciliation, lookup of the current holder of a non-custom function (for the
/// demote-or-fail decision when a profile ships its own standard macro), plus the insert/update
/// primitives. Customer CRUD goes through MacroManagementService, never through this repository.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface IMacroImportRepository
{
    Task<List<Macro>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys);

    Task<Macro?> GetActiveFunctionHolderAsync(MacroCategoryEnum category, MacroFunctionEnum function);

    void Add(Macro macro);

    void Update(Macro macro);
}
