// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardContextBuilder"/>.
/// Composes per-section sub-builders: contract days, keywords, preferences, blockers, locked works.
/// Will be filled incrementally in Tasks 2.1–2.4.
/// </summary>
public sealed class WizardContextBuilder : IWizardContextBuilder
{
    public Task<CoreWizardContext> BuildContextAsync(WizardContextRequest request, CancellationToken ct)
    {
        throw new NotImplementedException("Filled incrementally in Phase 2 Tasks 2.1-2.4");
    }
}
