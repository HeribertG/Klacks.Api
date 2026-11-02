// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Breaks;

/// <param name="Request">The absences to enter</param>
/// <param name="RemoveTakenWorks">False leaves the works standing, for a cover that decides about them itself</param>
public record BulkAddBreaksCommand(BulkAddBreaksRequest Request, bool RemoveTakenWorks = true) : IRequest<BulkBreaksResponse>;
