// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Qualifications;

/// <summary>
/// Returns the qualifications selectable in choice lists (shift requirements, employee
/// qualifications): rows without an industry plus rows of the industries activated via the
/// ACTIVE_INDUSTRIES setting. When the setting is missing or blank, all rows are returned.
/// </summary>
public record SelectionListQuery() : IRequest<IEnumerable<Qualification>>;
