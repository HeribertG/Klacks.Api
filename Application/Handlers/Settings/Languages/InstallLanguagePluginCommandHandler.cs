// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Installs a locally available language plugin: activates the installed-language setting and
/// imports its geo data, docs, synonyms, sentiment keywords and wake words. Returns false for
/// unknown codes, core languages or incompatible plugin versions.
/// </summary>
/// <param name="languagePluginService">Language plugin installation service.</param>

using Klacks.Api.Application.Commands.Settings.Languages;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Languages;

public class InstallLanguagePluginCommandHandler : IRequestHandler<InstallLanguagePluginCommand, bool>
{
    private readonly ILanguagePluginService _languagePluginService;

    public InstallLanguagePluginCommandHandler(ILanguagePluginService languagePluginService)
    {
        _languagePluginService = languagePluginService;
    }

    public async Task<bool> Handle(InstallLanguagePluginCommand request, CancellationToken cancellationToken)
    {
        return await _languagePluginService.InstallAsync(request.Code);
    }
}
