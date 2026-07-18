// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.SchedulingRules;

/// <summary>
/// Returns the scheduling rules selectable in choice lists (contract editing): rules without an
/// industry plus rules of the industries activated via the ACTIVE_INDUSTRIES setting. When the
/// setting is missing or blank, all rules are returned.
/// </summary>
public record SelectionListQuery() : IRequest<IEnumerable<SchedulingRuleResource>>;
