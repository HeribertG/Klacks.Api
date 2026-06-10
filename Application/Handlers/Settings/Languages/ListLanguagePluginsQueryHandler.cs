// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists all known language packs (core languages and discovered plugins) including their
/// installation state, version, coverage and translation count.
/// </summary>
/// <param name="languagePluginService">Language plugin discovery and installation state.</param>

using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Application.Queries.Settings.Languages;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Settings.Languages;

public class ListLanguagePluginsQueryHandler : IRequestHandler<ListLanguagePluginsQuery, IReadOnlyList<LanguagePluginInfo>>
{
    private readonly ILanguagePluginService _languagePluginService;

    public ListLanguagePluginsQueryHandler(ILanguagePluginService languagePluginService)
    {
        _languagePluginService = languagePluginService;
    }

    public Task<IReadOnlyList<LanguagePluginInfo>> Handle(ListLanguagePluginsQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_languagePluginService.GetAllPlugins());
    }
}
