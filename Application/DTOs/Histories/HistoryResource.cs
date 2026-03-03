// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Histories;

public class HistoryResource
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public DateTime? ValidFrom { get; set; }
    public int Type { get; set; }
    public string Data { get; set; } = string.Empty;
    public string OldData { get; set; } = string.Empty;
    public string NewData { get; set; } = string.Empty;
}