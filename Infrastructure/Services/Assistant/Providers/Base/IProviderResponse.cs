// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;

public interface IProviderResponse
{
    bool IsValid { get; }
    string GetContent();
    int GetInputTokens();
    int GetOutputTokens();
}