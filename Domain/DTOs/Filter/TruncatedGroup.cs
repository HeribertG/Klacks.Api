// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.DTOs.Filter;

public class TruncatedGroup : BaseTruncatedResult
{
    public ICollection<Group> Groups { get; set; } = null!;
}
