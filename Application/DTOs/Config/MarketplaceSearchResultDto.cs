// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Suchergebnis vom Marketplace mit Pagination.
/// </summary>
namespace Klacks.Api.Application.DTOs.Config;

public class MarketplaceSearchResultDto
{
    public List<MarketplacePackageDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
