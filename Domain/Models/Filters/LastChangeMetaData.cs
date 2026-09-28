// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Filters;

public class LastChangeMetaData
{
    public DateTime LastChangesDate { get; set; }
    public string Author { get; set; } = string.Empty;
}