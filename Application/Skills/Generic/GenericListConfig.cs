// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configuration record for generic list skill execution via reflection.
/// @param RepositoryInterface - DI interface name to resolve (e.g. "IBranchRepository")
/// @param Method - Repository method to call (e.g. "List" or "GetAllAsync")
/// @param Select - Property names to include in the result objects
/// @param OrderBy - Property name to sort results by (single field, backward-compat)
/// @param ResultProperty - Name of the result array property in the returned data
/// @param FilterIsDeleted - Whether to exclude soft-deleted entities
/// @param SearchField - Property name used for optional search-term filtering (single field, backward-compat)
/// @param SearchFields - Multiple property names for OR-based search-term filtering
/// @param OrderByFields - Multiple property names for multi-field sorting (first = primary, second = ThenBy, etc.)
/// @param AdditionalFilters - Extra filters applied from skill parameters to entity properties
/// </summary>
namespace Klacks.Api.Application.Skills.Generic;

public class GenericListConfig
{
    public string RepositoryInterface { get; set; } = string.Empty;
    public string Method { get; set; } = "List";
    public List<string> Select { get; set; } = new();
    public string OrderBy { get; set; } = "Name";
    public string ResultProperty { get; set; } = "Items";
    public bool FilterIsDeleted { get; set; } = true;
    public string? SearchField { get; set; } = "Name";
    public List<string> SearchFields { get; set; } = new();
    public List<string> OrderByFields { get; set; } = new();
    public List<GenericListFilter> AdditionalFilters { get; set; } = new();
}

public class GenericListFilter
{
    public string ParameterName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string MatchType { get; set; } = "equals";
}
