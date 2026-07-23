// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Requests an industry template preview: every scheduling rule and qualification the given
/// industry profile would bring, independent of the current ACTIVE_INDUSTRIES setting.
/// </summary>
/// <param name="Industry">Industry slug to preview, validated against IndustrySlugs.All</param>

using Klacks.Api.Application.DTOs.IndustryTemplates;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.IndustryTemplates;

public record PreviewQuery(string Industry) : IRequest<IndustryTemplatePreviewResource>;
