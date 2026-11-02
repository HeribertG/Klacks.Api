// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class ScenarioVariantResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid GroupId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }

    public Guid Token { get; set; }

    public int Status { get; set; }

    public bool IsExpired { get; set; }

    public int ShiftCount { get; set; }
}
