// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ClientShiftPreferences;

public record GetAvailableShiftsQuery(Guid ClientId) : IRequest<List<AvailableShiftResource>>;
