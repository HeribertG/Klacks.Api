// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Staffs;

public class ClientImageResource
{
    public Guid Id { get; set; }
    public Guid ClientId { get; set; }
    public string ImageData { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public long FileSize { get; set; }
}
