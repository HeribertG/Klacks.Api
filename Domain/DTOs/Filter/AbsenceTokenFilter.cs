// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.Filter;

public class AbsenceTokenFilter
{
    public Guid Id { get; set; }
    
    public string Name { get; set; } = string.Empty;

    public bool Checked { get; set; }
}
