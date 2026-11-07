// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Staffs;

public class CreatedStandInResource
{
    public Guid Id { get; set; }

    public int IdNumber { get; set; }

    public string Name { get; set; } = string.Empty;
}
