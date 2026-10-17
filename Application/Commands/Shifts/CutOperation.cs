// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Commands.Shifts;

public class CutOperation
{
    public const string UpdateType = "UPDATE";

    public const string CreateType = "CREATE";

    public string Type { get; set; } = string.Empty;

    public string ParentId { get; set; } = string.Empty;

    public ShiftResource Data { get; set; } = null!;
}
