// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What the administrator wants a new contract to look like. Every member is optional; only members that were
/// given take part in the comparison against existing contract templates.
/// </summary>
/// <param name="PaymentInterval">Wished payment interval</param>
/// <param name="GuaranteedHours">Wished fixed guaranteed hours per interval (workload path 1)</param>
/// <param name="Percent">Wished workload percent of the company-wide value (workload path 2)</param>
/// <param name="FullTime">Wished full-time reference hours</param>
/// <param name="PerformsShiftWork">Wished shift-work flag</param>
/// <param name="Workdays">Wished working weekdays</param>
/// <param name="Region">Wished holiday-calendar region</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Associations;

public sealed record ContractTemplateWish(
    PaymentInterval? PaymentInterval,
    decimal? GuaranteedHours,
    decimal? Percent,
    decimal? FullTime,
    bool? PerformsShiftWork,
    IReadOnlySet<DayOfWeek>? Workdays,
    ContractTemplateRegionWish? Region)
{
    public int WishCount =>
        (PaymentInterval.HasValue ? 1 : 0)
        + (GuaranteedHours.HasValue ? 1 : 0)
        + (Percent.HasValue ? 1 : 0)
        + (FullTime.HasValue ? 1 : 0)
        + (PerformsShiftWork.HasValue ? 1 : 0)
        + (Workdays != null ? 1 : 0)
        + (Region != null ? 1 : 0);
}
