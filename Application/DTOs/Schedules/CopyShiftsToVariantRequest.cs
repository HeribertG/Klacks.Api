// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class CopyShiftsToVariantRequest
{
    public IReadOnlyList<Guid>? ShiftIds { get; set; }

    public Guid? SourceGroupId { get; set; }
}
