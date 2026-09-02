// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Closes the recipe-run funnel from the other end (W1.5): a plan that is abandoned mid-flow leaves
/// its row on Running forever, because only completion and abort write an end state. The per-tick
/// sweep flips every Running row untouched for longer than RecipeRunDefaults.ExpireAfter to Expired.
/// Set-based, so an arbitrarily large backlog costs one UPDATE.
/// </summary>
/// <param name="repository">Recipe-run telemetry store.</param>
/// <param name="timeProvider">Clock, so a test can drive the cutoff instead of waiting a day.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class RecipeRunExpirySweep : IRecipeRunExpirySweep
{
    private readonly IRecipeRunRepository _repository;
    private readonly TimeProvider _timeProvider;

    public RecipeRunExpirySweep(IRecipeRunRepository repository, TimeProvider timeProvider)
    {
        _repository = repository;
        _timeProvider = timeProvider;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return await _repository.ExpireStaleAsync(now - RecipeRunDefaults.ExpireAfter, now, cancellationToken);
    }
}
