// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.DTOs;

public class HttpResultResource
{
    public bool Success { get; set; }

    public string Messages { get; set; } = string.Empty;
}