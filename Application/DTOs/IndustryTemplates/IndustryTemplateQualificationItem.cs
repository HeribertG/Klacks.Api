// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.DTOs.IndustryTemplates;

public class IndustryTemplateQualificationItem
{
    public MultiLanguage Name { get; set; } = new();

    public MultiLanguage? Description { get; set; }
}
