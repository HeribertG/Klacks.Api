// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs.Filter;
namespace Klacks.Api.Application.DTOs.Filter;

public class FilterHistoryResource : BaseFilter
{
    public Guid Key { get; set; }
}
