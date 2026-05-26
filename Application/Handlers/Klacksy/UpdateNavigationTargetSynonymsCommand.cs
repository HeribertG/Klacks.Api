// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command and handler for updating synonyms for a navigation target, persisting to the database.
/// </summary>
/// <param name="TargetId">The unique identifier of the navigation target to update</param>
/// <param name="Locale">The locale whose synonyms are being updated (e.g. "de", "en", "fr")</param>
/// <param name="Synonyms">The new set of synonyms for this locale</param>
/// <param name="Status">The new synonym status (e.g. "generated", "approved")</param>
namespace Klacks.Api.Application.Handlers.Klacksy;

using Klacks.Api.Application.Klacksy;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

public record UpdateNavigationTargetSynonymsCommand(string TargetId, string Locale, string[] Synonyms, string Status) : IRequest<bool>;

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
            cancellationToken);

        _cache.Invalidate();
        return true;
    }
}
