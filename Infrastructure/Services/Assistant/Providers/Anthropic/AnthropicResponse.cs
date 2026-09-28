// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Anthropic;

public class AnthropicResponse
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Role { get; set; }
    public List<AnthropicContent>? Content { get; set; }
    public AnthropicUsage? Usage { get; set; }
}