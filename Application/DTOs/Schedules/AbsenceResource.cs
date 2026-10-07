// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.DTOs.Schedules;

public class AbsenceResource
{
    public MultiLanguage Abbreviation { get; set; } = null!;

    public string Color { get; set; } = string.Empty;

    public int DefaultLength { get; set; } = 0;

    public double DefaultValue { get; set; }

    public MultiLanguage Description { get; set; } = null!;

    public bool HideInGantt { get; set; }

    public Guid Id { get; set; }

    public Guid? MacroId { get; set; }

    public MultiLanguage Name { get; set; } = null!;

    public bool Undeletable { get; set; }

    public bool WithHoliday { get; set; }

    public bool WithSaturday { get; set; }

    public bool WithSunday { get; set; }

    public bool AppliesToContainer { get; set; }

    public bool IsUnpaid { get; set; }

    public bool IsOnCall { get; set; }
}
