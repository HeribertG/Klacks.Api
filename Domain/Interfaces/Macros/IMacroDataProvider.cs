// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IMacroDataProvider
{
    Task<MacroData> GetMacroDataAsync(Work work);
    Task<MacroData> GetMacroDataForWorkChangeAsync(WorkChange workChange, Work work);
}
