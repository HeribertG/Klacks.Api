// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.DTOs.Filter;

public class AbsenceTokenFilter
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public bool Checked { get; set; }
}
