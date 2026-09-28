// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.DTOs.IndustryTemplates;

public class IndustryTemplateQualificationItem
{
    public MultiLanguage Name { get; set; } = new();

    public MultiLanguage? Description { get; set; }
}
