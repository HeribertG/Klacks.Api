// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Reports;

public class CellBorderStyle
{
    public BorderSideStyle Top { get; set; } = new();
    public BorderSideStyle Right { get; set; } = new();
    public BorderSideStyle Bottom { get; set; } = new();
    public BorderSideStyle Left { get; set; } = new();
}
