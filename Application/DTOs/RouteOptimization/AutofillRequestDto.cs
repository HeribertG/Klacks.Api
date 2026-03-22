// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// POST body for the autofill endpoint.
/// </summary>
/// <param name="ContainerId">Container ID</param>
/// <param name="Weekday">Weekday (0-6)</param>
/// <param name="IsHoliday">Holiday variant</param>
/// <param name="StartBase">Start address</param>
/// <param name="EndBase">End address</param>
/// <param name="FromTime">Time window start (HH:mm)</param>
/// <param name="UntilTime">Time window end (HH:mm)</param>
/// <param name="TransportMode">Transport mode</param>
/// <param name="TimeRangeTolerance">Tolerance (0.0-1.0)</param>
/// <param name="TimeBlocks">Optional time blocks (breaks etc.)</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class AutofillRequestDto
{
    public Guid ContainerId { get; set; }
    public int Weekday { get; set; }
    public bool IsHoliday { get; set; }
    public string StartBase { get; set; } = string.Empty;
    public string EndBase { get; set; } = string.Empty;
    public string FromTime { get; set; } = string.Empty;
    public string UntilTime { get; set; } = string.Empty;
    public ContainerTransportMode TransportMode { get; set; } = ContainerTransportMode.ByCar;
    public double TimeRangeTolerance { get; set; } = 0.5;
    public List<TimeBlockDto> TimeBlocks { get; set; } = new();
}
