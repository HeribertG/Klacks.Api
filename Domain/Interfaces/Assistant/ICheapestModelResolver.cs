// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ICheapestModelResolver
{
    Task<(LLMModel? Model, ILLMProvider? Provider)> ResolveAsync(CancellationToken cancellationToken = default);
}
