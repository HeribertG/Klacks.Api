// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads a single applied company rule from the registry by id.
/// </summary>
/// <param name="Id">Registry id of the company rule.</param>

using System;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.CompanyRules;

public record GetCompanyRuleQuery(Guid Id) : IRequest<CompanyRuleResource?>;
