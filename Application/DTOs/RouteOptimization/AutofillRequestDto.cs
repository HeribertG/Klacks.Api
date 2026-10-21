// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// POST body for the autofill endpoint.
/// </summary>
/// <param name="ContainerId">Container ID</param>
/// <param name="Weekday">Weekday (0-6)</param>
/// <param name="IsHoliday">Holiday flag of the container template slot that is filled: false or omitted = the normal
/// template of Weekday, true = a holiday template (see IsWeekdayAndHoliday)</param>
/// <param name="IsWeekdayAndHoliday">Only read with IsHoliday: true = the holiday template of Weekday (applies on public
/// holidays falling on Weekday), false or omitted = the generic holiday template (applies on every public holiday; its
/// Weekday is only a storage key)</param>
/// <param name="StartBase">Start address</param>
/// <param name="EndBase">End address</param>
/// <param name="FromTime">Time window start (HH:mm)</param>
/// <param name="UntilTime">Time window end (HH:mm)</param>
/// <param name="TransportMode">Transport mode</param>
/// <param name="TimeRangeTolerance">Strictness of the task TimeRange windows (0.0-1.0): 1.0 = strict, lower values widen
/// the window by (1 - value) x its length, 0 disables the check; the UI slider sends 0 at "flexible", 1 at "exact"</param>
/// <param name="TimeBlocks">Optional time blocks (breaks etc.)</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class AutofillRequestDto
{
    public Guid ContainerId { get; set; }
    public int Weekday { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsWeekdayAndHoliday { get; set; }
    public string StartBase { get; set; } = string.Empty;
    public string EndBase { get; set; } = string.Empty;
    public string FromTime { get; set; } = string.Empty;
    public string UntilTime { get; set; } = string.Empty;
    public ContainerTransportMode TransportMode { get; set; } = ContainerTransportMode.ByCar;
    public double TimeRangeTolerance { get; set; } = 0.5;
    public List<TimeBlockDto> TimeBlocks { get; set; } = new();
    public List<Guid> AdditionalAvailableWorkIds { get; set; } = new();
}
