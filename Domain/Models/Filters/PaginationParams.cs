// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Filters;

public class PaginationParams
{
    public int PageIndex { get; set; } = 0;

    public int PageSize { get; set; } = PagingDefaults.DefaultPageSize;

    public string SortBy { get; set; } = string.Empty;

    public bool IsDescending { get; set; } = false;

    
    public int Skip => PageIndex * PageSize;

    public int Take => PageSize;
}