// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Associations;

using Klacks.Api.Domain.DTOs.Filter;
namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedGroupResource : BaseTruncatedResult
{
    public ICollection<GroupResource> Groups { get; set; } = null!;
}
