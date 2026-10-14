// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;

namespace Klacks.Api.Application.Interfaces.Schedules.AutoWizard;

/// <summary>
/// Tells every open schedule whose user may see the run's group that an autofill run started or ended.
/// </summary>
public interface IAutofillRunPresenceNotifier
{
    Task NotifyStartedAsync(AutofillRunPresenceDto run);

    Task NotifyEndedAsync(AutofillRunPresenceDto run);
}
