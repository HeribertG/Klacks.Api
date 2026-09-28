// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Shared;

public class OpenAIChoice
{
    public OpenAIMessage? Message { get; set; }
    public string? FinishReason { get; set; }
}