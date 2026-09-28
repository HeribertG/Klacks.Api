// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Histories;

namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedHistory
{
    public int MaxItems { get; set; }
    
    public int MaxPages { get; set; }
    
    public int CurrentPage { get; set; }
    
    public ICollection<History>? Histories { get; set; }
}
