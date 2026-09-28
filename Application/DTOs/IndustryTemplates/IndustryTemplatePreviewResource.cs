// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.IndustryTemplates;

public class IndustryTemplatePreviewResource
{
    public string Industry { get; set; } = string.Empty;

    public List<IndustryTemplateSchedulingRuleItem> SchedulingRules { get; set; } = new();

    public List<IndustryTemplateQualificationItem> Qualifications { get; set; } = new();
}
