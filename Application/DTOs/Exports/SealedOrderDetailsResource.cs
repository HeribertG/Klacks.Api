// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detail preview of a sealed order for the export dialog: order metadata, ERP references,
/// customer information and all work entries in the requested range regardless of lock level.
/// @param WorkEntries - All non-deleted works of the order and its descendants, sorted by date and start time
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class SealedOrderDetailsResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Abbreviation { get; set; } = string.Empty;

    public string? SourceSystemId { get; set; }

    public string? ExternalOrderReference { get; set; }

    public Guid? CustomerId { get; set; }

    public int? CustomerNumber { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerExternalReference { get; set; }

    public List<SealedOrderWorkEntryResource> WorkEntries { get; set; } = [];
}
