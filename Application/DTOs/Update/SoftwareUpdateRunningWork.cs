// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Update;

/// <summary>
/// Counts of the planning work in progress in this server process. A run can show up in several counts (an autofill run
/// drives wizard and harmonizer stages), so they are reported side by side and never added up.
/// </summary>
/// <param name="AutofillRuns">Autofill runs in progress, all groups</param>
/// <param name="WizardJobs">Wizard jobs running</param>
/// <param name="HarmonizerJobs">Harmonizer jobs running</param>
/// <param name="HolisticHarmonizerJobs">Holistic harmonizer jobs running</param>
public sealed record SoftwareUpdateRunningWork(
    int AutofillRuns,
    int WizardJobs,
    int HarmonizerJobs,
    int HolisticHarmonizerJobs)
{
    public bool Any => AutofillRuns > 0 || WizardJobs > 0 || HarmonizerJobs > 0 || HolisticHarmonizerJobs > 0;
}
