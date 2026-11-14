// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Breaks;

/// <summary>
/// Query that previews which works an absence entry would remove.
/// </summary>
/// <param name="Request">The absences about to be entered</param>
public record PreviewTakenWorksQuery(BulkAddBreaksRequest Request) : IRequest<IReadOnlyList<RemovableWork>>;
