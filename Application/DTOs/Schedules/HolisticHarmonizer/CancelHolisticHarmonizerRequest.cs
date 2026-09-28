// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <param name="JobId">Identifier of the running job to cancel; obtained from <c>/Start</c>.</param>
public sealed record CancelHolisticHarmonizerRequest(Guid JobId);
