// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class SkillEffectivenessRecipeFunnelRow
{
    public string RecipeName { get; set; } = string.Empty;

    public int Started { get; set; }

    public int Running { get; set; }

    public int Completed { get; set; }

    public int Aborted { get; set; }

    public int Expired { get; set; }
}
