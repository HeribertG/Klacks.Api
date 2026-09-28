// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;

public interface IProviderRequest
{
    string Model { get; set; }
    double Temperature { get; set; }
    int MaxTokens { get; set; }
}