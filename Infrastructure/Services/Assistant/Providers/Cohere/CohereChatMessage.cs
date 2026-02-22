// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Cohere;

public class CohereChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}