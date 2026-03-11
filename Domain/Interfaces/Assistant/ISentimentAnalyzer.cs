// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISentimentAnalyzer
{
    SentimentResult AnalyzeSentiment(string userMessage);
    void ReloadKeywords();
}
