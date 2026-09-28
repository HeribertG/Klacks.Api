// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Qualifications;

/// <summary>
/// Returns all non-deleted qualifications ordered by name.
/// </summary>
public record ListQuery() : IRequest<IEnumerable<Qualification>>;
