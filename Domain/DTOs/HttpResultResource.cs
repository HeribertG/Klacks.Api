// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs;

public class HttpResultResource
{
    public bool Success { get; set; }

    public string Messages { get; set; } = string.Empty;
}