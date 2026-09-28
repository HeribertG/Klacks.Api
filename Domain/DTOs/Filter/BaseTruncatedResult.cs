// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.Filter;

public class BaseTruncatedResult
{
    public int MaxItems { get; set; }
    
    public int MaxPages { get; set; }
    
    public int CurrentPage { get; set; }
    
    public int FirstItemOnPage { get; set; }
}
