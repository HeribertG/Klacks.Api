// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Reports;

public class FreeTextRow
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Position { get; set; } = "after";
    public FieldStyle Style { get; set; } = new();
}
