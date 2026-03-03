// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Reports;

public class FreeTextRowResource
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Position { get; set; } = "after";
    public FieldStyleResource Style { get; set; } = new();
}
