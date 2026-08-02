// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Reports;

public class StyleCondition
{
    public string Expression { get; set; } = string.Empty;

    public string? TextColor { get; set; }

    public string? BackgroundColor { get; set; }

    public bool? Bold { get; set; }

    public bool? Italic { get; set; }
}
