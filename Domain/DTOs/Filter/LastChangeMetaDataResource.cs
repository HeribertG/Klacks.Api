// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.Filter;

public class LastChangeMetaDataResource
{
    public DateTime LastChangesDate { get; set; }
    
    public string Autor { get; set; } = string.Empty;
}
