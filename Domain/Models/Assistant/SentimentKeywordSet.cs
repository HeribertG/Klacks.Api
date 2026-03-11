// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class SentimentKeywordSet : BaseEntity
{
    public string Language { get; set; } = string.Empty;
    public Dictionary<string, List<string>> Keywords { get; set; } = new();
    public string Source { get; set; } = string.Empty;
}
