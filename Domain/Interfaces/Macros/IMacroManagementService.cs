// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IMacroManagementService
{
    Task<Macro> AddMacroAsync(Macro macro);

    Task<Macro> DeleteMacroAsync(Guid id);

    Task<Macro> GetMacroAsync(Guid id);

    Task<List<Macro>> GetMacroListAsync();

    Task<bool> MacroExistsAsync(Guid id);

    Task<Macro> UpdateMacroAsync(Macro macro, bool byAssistant);
}