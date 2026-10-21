// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class SkillEffectivenessEvalRun
{
    public string Goldset { get; set; } = string.Empty;

    public string? Model { get; set; }

    public decimal CompositeScore { get; set; }

    public int ItemsTotal { get; set; }

    public int ItemsPassed { get; set; }

    public DateTime? CreateTime { get; set; }
}
