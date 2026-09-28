// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILLMService
{
    Task<LLMResponse> ProcessAsync(LLMContext context, CancellationToken cancellationToken = default);

    IAsyncEnumerable<SseChunk> ProcessStreamAsync(LLMContext context, CancellationToken cancellationToken = default);
}