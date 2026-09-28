// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

public sealed record GroupingUnitSection(
    IReadOnlyList<GroupingUnitView> Units,
    GroupingUnitTotals Totals,
    IReadOnlyList<string> BlockingCauses);
