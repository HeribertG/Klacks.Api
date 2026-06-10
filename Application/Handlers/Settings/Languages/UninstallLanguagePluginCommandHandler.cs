// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Uninstalls a language plugin: deactivates the installed-language setting and removes its
/// imported synonyms, sentiment keywords, geo data and docs. Returns false for core languages.
/// </summary>
/// <param name="languagePluginService">Language plugin installation service.</param>

using Klacks.Api.Application.Commands.Settings.Languages;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Languages;

public class UninstallLanguagePluginCommandHandler : IRequestHandler<UninstallLanguagePluginCommand, bool>
{
    private readonly ILanguagePluginService _languagePluginService;

    public UninstallLanguagePluginCommandHandler(ILanguagePluginService languagePluginService)
    {
        _languagePluginService = languagePluginService;
    }

    public async Task<bool> Handle(UninstallLanguagePluginCommand request, CancellationToken cancellationToken)
    {
        return await _languagePluginService.UninstallAsync(request.Code);
    }
}
