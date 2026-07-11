// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IClientFuzzySearchService
{
    /// <summary>
    /// Ranks clients whose name fields are similar to the query — trigram similarity for typos
    /// and partial names, Kölner Phonetik token overlap for misheard spoken names. Intended as
    /// the fallback when the exact/contains search finds nothing. Results are ordered best-first.
    /// </summary>
    /// <param name="query">Raw user query (name, name parts; possibly STT-damaged)</param>
    /// <param name="limit">Maximum number of ranked clients to return</param>
    Task<List<Client>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default);
}
