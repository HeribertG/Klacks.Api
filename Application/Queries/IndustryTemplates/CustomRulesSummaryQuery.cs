// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Requests the count of custom (non-industry) scheduling rules, i.e. rules with an empty
/// Industry, so the UI can warn before switching ACTIVE_INDUSTRIES away from "custom" when custom
/// rules already exist.
/// </summary>

using Klacks.Api.Application.DTOs.IndustryTemplates;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.IndustryTemplates;

public record CustomRulesSummaryQuery : IRequest<CustomRulesSummaryResource>;
