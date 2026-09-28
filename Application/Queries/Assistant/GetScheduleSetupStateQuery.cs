// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

/// <summary>
/// Query for the installation-wide setup snapshot along the order -> shift -> assignment chain.
/// Read-only, no parameters.
/// </summary>
public class GetScheduleSetupStateQuery : IRequest<ScheduleSetupStateResource>
{
}
