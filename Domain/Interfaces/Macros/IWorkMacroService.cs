// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IWorkMacroService
{
    /// <summary>
    /// Runs the macro calculation for a work and reports whether it succeeded.
    /// </summary>
    /// <returns>False when the calculation failed and the stored values stay as they were</returns>
    Task<bool> ProcessWorkMacroAsync(Work work);

    /// <summary>
    /// Runs the macro calculation for a changed work and reports whether it succeeded.
    /// </summary>
    /// <returns>False when the calculation failed and the stored values stay as they were</returns>
    Task<bool> ProcessWorkChangeMacroAsync(WorkChange workChange);
}
