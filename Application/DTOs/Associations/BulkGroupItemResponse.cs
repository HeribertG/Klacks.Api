// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Associations;

public class BulkGroupItemResponse
{
    /// <summary>How many rows were created and read back inside the same transaction.</summary>
    public int AddedCount { get; set; }

    public ICollection<Guid> AddedIds { get; set; } = new List<Guid>();
}
