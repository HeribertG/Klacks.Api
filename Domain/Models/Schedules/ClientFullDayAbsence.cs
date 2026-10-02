// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record ClientFullDayAbsence(Guid ClientId, DateOnly Date);
