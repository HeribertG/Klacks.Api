// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Schedules;

/// <summary>
/// Asks for the autofill runs in progress that the calling user may see.
/// </summary>
public sealed record GetActiveAutofillRunsQuery : IRequest<IReadOnlyList<AutofillRunPresenceDto>>;
