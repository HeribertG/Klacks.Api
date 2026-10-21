// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Queries.ClientShiftPreferences;

public class AvailableShiftResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Abbreviation { get; set; }
}
