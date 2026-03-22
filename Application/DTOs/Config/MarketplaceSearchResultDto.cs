// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Search result from the Marketplace with pagination.
/// </summary>
namespace Klacks.Api.Application.DTOs.Config;

public class MarketplaceSearchResultDto
{
    public List<MarketplacePackageDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
