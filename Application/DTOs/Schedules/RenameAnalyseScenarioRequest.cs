// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Request DTO for renaming an existing AnalyseScenario.
/// </summary>
/// <param name="Name">New name for the scenario</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public class RenameAnalyseScenarioRequest
{
    public string Name { get; set; } = string.Empty;
}
