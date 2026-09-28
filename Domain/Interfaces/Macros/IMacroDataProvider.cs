// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IMacroDataProvider
{
    Task<MacroData> GetMacroDataAsync(Work work);
    Task<MacroData> GetMacroDataForWorkChangeAsync(WorkChange workChange, Work work);
    Task<MacroData> GetMacroDataForBreakAsync(Break breakEntry, int? paymentInterval = null);
}
