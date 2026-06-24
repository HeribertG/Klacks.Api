// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs.Filter;

namespace Klacks.Api.Application.DTOs.Staffs;

/// <summary>
/// Request payload for exporting a filtered and optionally selected list of clients.
/// </summary>
/// <param name="Filter">The active client filter (search, type, membership scope, etc.)</param>
/// <param name="Selection">IDs to include (when SelectAll=false) or to exclude (when InvertedSelection=true)</param>
/// <param name="SelectAll">When true, all filter-matching clients are exported unless overridden by InvertedSelection</param>
/// <param name="InvertedSelection">When true, Selection contains IDs to exclude from the full filtered result</param>
public class ExportClientRequest
{
    public FilterResource Filter { get; set; } = new();
    public List<Guid> Selection { get; set; } = [];
    public bool SelectAll { get; set; }
    public bool InvertedSelection { get; set; }
}
