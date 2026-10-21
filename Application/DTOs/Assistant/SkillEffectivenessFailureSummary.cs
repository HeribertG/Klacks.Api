// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class SkillEffectivenessFailureSummary
{
    public int TotalRows { get; set; }

    public int NotFound { get; set; }

    public int PermissionDenied { get; set; }

    public int ParameterInvalid { get; set; }

    public int GateHold { get; set; }

    public int UiActionContext { get; set; }

    public int Exception { get; set; }

    public double HallucinationRate { get; set; }
}
