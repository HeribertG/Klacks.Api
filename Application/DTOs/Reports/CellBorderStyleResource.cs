// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Reports;

public class CellBorderStyleResource
{
    public BorderSideStyleResource Top { get; set; } = new();
    public BorderSideStyleResource Right { get; set; } = new();
    public BorderSideStyleResource Bottom { get; set; } = new();
    public BorderSideStyleResource Left { get; set; } = new();
}
