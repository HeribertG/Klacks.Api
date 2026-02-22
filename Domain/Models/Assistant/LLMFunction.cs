// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public class LLMFunction
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Dictionary<string, object> Parameters { get; set; } = new();

    public List<string> RequiredParameters { get; set; } = new();
}
