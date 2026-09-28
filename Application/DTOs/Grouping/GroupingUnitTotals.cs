// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingUnitTotals(int PlanningUnits, int UnitsWithGaps, int UnitsNotListed);
