// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.MCP;

public class ResourceContent
{
    public string Uri { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}