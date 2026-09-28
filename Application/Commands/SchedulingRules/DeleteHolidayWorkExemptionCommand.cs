// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.SchedulingRules;

/// <summary>
/// Removes a holiday-work exemption.
/// </summary>
/// <param name="Id">The exemption to remove</param>
public sealed record DeleteHolidayWorkExemptionCommand(Guid Id) : IRequest<bool>;
