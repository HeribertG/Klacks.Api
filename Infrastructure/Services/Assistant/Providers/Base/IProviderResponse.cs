// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;

public interface IProviderResponse
{
    bool IsValid { get; }
    string GetContent();
    int GetInputTokens();
    int GetOutputTokens();
}