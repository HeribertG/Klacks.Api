// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.IndustryTemplates;

public class IndustryTemplatePreviewResource
{
    public string Industry { get; set; } = string.Empty;

    public List<IndustryTemplateSchedulingRuleItem> SchedulingRules { get; set; } = new();

    public List<IndustryTemplateQualificationItem> Qualifications { get; set; } = new();
}
