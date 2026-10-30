// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

/// <summary>
/// Why a located task is not on the planned tour.
/// </summary>
public enum ContainerAutofillOmissionReason
{
    TooLongForTheDay = 0,
    TimeWindowConflict = 1,
    NoRoomOnTour = 2,
}
