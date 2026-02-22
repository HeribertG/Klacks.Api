// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Filter;

public class LastChangeMetaDataResource
{
    public DateTime LastChangesDate { get; set; }
    
    public string Autor { get; set; } = string.Empty;
}
