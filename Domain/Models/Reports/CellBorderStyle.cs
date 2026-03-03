// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Reports;

public class CellBorderStyle
{
    public BorderSideStyle Top { get; set; } = new();
    public BorderSideStyle Right { get; set; } = new();
    public BorderSideStyle Bottom { get; set; } = new();
    public BorderSideStyle Left { get; set; } = new();
}
