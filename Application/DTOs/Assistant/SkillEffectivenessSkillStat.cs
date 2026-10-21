// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class SkillEffectivenessSkillStat
{
    public string SkillName { get; set; } = string.Empty;

    public int Calls { get; set; }

    public int Successes { get; set; }

    public int Failures { get; set; }

    public double SuccessRate { get; set; }
}
