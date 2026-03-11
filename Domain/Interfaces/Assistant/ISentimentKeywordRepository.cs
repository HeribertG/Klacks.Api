// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISentimentKeywordRepository
{
    Task<List<SentimentKeywordSet>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(string language, Dictionary<string, List<string>> keywords, string source, CancellationToken cancellationToken = default);
    Task DeleteByLanguageAsync(string language, CancellationToken cancellationToken = default);
}
