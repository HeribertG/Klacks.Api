// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Links a scenario to a variant (key) it carries.
/// </summary>
/// <param name="ScenarioToken">Token of the scenario that carries the variant</param>
/// <param name="VariantId">Id of the AnalyseScenario row with Kind = Variant</param>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class AnalyseScenarioVariant : BaseEntity
{
    public Guid ScenarioToken { get; set; }

    public Guid VariantId { get; set; }

    public AnalyseScenario? Variant { get; set; }
}
