// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Per-(client, date, scenario) record of a soft-constraint that Wizard 1 had to relax
/// during scheduling (Round 2 of the auctioneer). Persisted at apply-time so Wizard 2
/// (Harmonizer) can prioritise these slots when looking for harmonisation opportunities.
/// </summary>
public class WorkSoftening : BaseEntity
{
    public Guid ClientId { get; set; }

    public DateOnly CurrentDate { get; set; }

    public SofteningKind Kind { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public string Hint { get; set; } = string.Empty;

    public Guid? AnalyseToken { get; set; }
}
