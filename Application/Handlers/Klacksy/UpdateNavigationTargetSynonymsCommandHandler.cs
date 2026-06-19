// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating synonyms for a navigation target, persisting to the database.
/// </summary>

namespace Klacks.Api.Application.Handlers.Klacksy;

using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

public sealed class UpdateNavigationTargetSynonymsCommandHandler : IRequestHandler<UpdateNavigationTargetSynonymsCommand, bool>
{
    private readonly INavigationTargetCacheService _cache;
    private readonly INavigationTargetSynonymRepository _synonymRepository;

    public UpdateNavigationTargetSynonymsCommandHandler(
        INavigationTargetCacheService cache,
        INavigationTargetSynonymRepository synonymRepository)
    {
        _cache = cache;
        _synonymRepository = synonymRepository;
    }

    public async Task<bool> Handle(UpdateNavigationTargetSynonymsCommand command, CancellationToken cancellationToken)
    {
        var target = _cache.GetById(command.TargetId);
        if (target == null)
            throw new KeyNotFoundException(command.TargetId);

        await _synonymRepository.ReplaceForTargetLanguageAsync(
            command.TargetId,
            command.Locale,
            command.Synonyms,
            SynonymSources.User,
            cancellationToken);

        _cache.Invalidate();
        return true;
    }
}
