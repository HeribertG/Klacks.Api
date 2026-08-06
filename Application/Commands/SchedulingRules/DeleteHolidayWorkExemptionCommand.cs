// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.SchedulingRules;

/// <summary>
/// Removes a holiday-work exemption.
/// </summary>
/// <param name="Id">The exemption to remove</param>
public sealed record DeleteHolidayWorkExemptionCommand(Guid Id) : IRequest<bool>;
