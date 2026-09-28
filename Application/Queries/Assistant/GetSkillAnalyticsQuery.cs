// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Queries.Assistant;

public record GetSkillAnalyticsQuery(int Days) : IRequest<SkillAnalyticsDto>;
