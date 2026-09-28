// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IWorkMacroService
{
    Task ProcessWorkMacroAsync(Work work);
    Task ProcessWorkChangeMacroAsync(WorkChange workChange);
}
