// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The numeric and boolean overrides an administrator gave for a contract created from a template. Every member
/// is optional; null means "not given, keep the template value". TryRead refuses a present but unreadable
/// argument instead of treating it as not given.
/// </summary>
/// <param name="GuaranteedHours">Fixed guaranteed hours per interval (workload path 1)</param>
/// <param name="Percent">Workload percent of the company-wide value (workload path 2)</param>
/// <param name="MinimumHours">Minimum hours of the band</param>
/// <param name="MaximumHours">Maximum hours of the band</param>
/// <param name="FullTime">Full-time reference hours</param>
/// <param name="PerformsShiftWork">Shift-work flag</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Contracts;

public sealed record ContractTemplateOverrides(
    decimal? GuaranteedHours,
    decimal? Percent,
    decimal? MinimumHours,
    decimal? MaximumHours,
    decimal? FullTime,
    bool? PerformsShiftWork)
{
    public static bool TryRead(
        Dictionary<string, object> parameters, out ContractTemplateOverrides overrides, out string? error)
    {
        overrides = new ContractTemplateOverrides(null, null, null, null, null, null);

        if (!ContractSkillInput.TryReadDecimal(parameters, ContractFieldNames.GuaranteedHours, out var guaranteedHours, out error)
            || !ContractSkillInput.TryReadDecimal(parameters, ContractFieldNames.Percent, out var percent, out error)
            || !ContractSkillInput.TryReadDecimal(parameters, ContractFieldNames.MinimumHours, out var minimumHours, out error)
            || !ContractSkillInput.TryReadDecimal(parameters, ContractFieldNames.MaximumHours, out var maximumHours, out error)
            || !ContractSkillInput.TryReadDecimal(parameters, ContractFieldNames.FullTime, out var fullTime, out error)
            || !ContractSkillInput.TryReadBool(parameters, ContractFieldNames.PerformsShiftWork, out var performsShiftWork, out error))
        {
            return false;
        }

        overrides = new ContractTemplateOverrides(
            guaranteedHours, percent, minimumHours, maximumHours, fullTime, performsShiftWork);
        return true;
    }
}
