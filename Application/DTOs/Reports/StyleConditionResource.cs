// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Reports;

public class StyleConditionResource
{
    public string Expression { get; set; } = string.Empty;

    public string? TextColor { get; set; }

    public string? BackgroundColor { get; set; }

    public bool? Bold { get; set; }

    public bool? Italic { get; set; }
}
