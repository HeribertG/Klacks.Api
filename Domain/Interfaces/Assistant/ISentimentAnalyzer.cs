// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISentimentAnalyzer
{
    Task<SentimentResult> AnalyzeSentimentAsync(string userMessage);
    void ReloadKeywords();
}
