// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A named qualification / skill an employee can hold and a shift can require
/// (e.g. "Forklift licence", "First aid"). Soft-deletable master entity.
/// Name and Description are stored as JSONB MultiLanguage values.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Staffs;

public class Qualification : BaseEntity
{
    public MultiLanguage Name { get; set; } = new();

    public MultiLanguage? Description { get; set; }

    public string? Emoji { get; set; }

    public bool IsTimeLimited { get; set; }
}
