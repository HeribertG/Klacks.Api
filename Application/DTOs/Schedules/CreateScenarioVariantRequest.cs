// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class CreateScenarioVariantRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid GroupId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }
}
