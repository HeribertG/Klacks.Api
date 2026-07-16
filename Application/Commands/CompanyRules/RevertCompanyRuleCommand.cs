// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reverts a previously applied company rule: restores the overwritten surcharge settings from the
/// snapshot or soft-deletes the counter rule / custom macro it created, then soft-deletes the registry
/// row. Missing targets are tolerated.
/// </summary>
/// <param name="Id">Registry id of the company rule to revert.</param>

using System;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.CompanyRules;

public record RevertCompanyRuleCommand(Guid Id) : IRequest<CompanyRuleResource?>;
