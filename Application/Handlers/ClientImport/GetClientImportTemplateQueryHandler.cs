// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the downloadable xlsx template of the employee import with the column headers in the
/// requested UI language (one of the supported language packs, matched case-insensitively). Without a
/// requested language the first Accept-Language entry that is supported wins, matched by its full tag
/// (zh-TW) and then by its primary subtag (de-CH -> de); English is the last resort. An explicitly
/// requested but unsupported language is still rejected.
/// </summary>
/// <param name="catalog">Header vocabulary; the first synonym per target is the template header</param>
/// <param name="templateBuilder">Writes the xlsx file</param>

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces.ClientImport;
using Klacks.Api.Application.Queries.ClientImport;
using Klacks.Api.Application.Services.ClientImport;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientImport;

public class GetClientImportTemplateQueryHandler : BaseHandler, IRequestHandler<GetClientImportTemplateQuery, ClientImportTemplateFile>
{
    private const char LanguageSubtagSeparator = '-';

    private readonly ClientImportSynonymCatalog _catalog;
    private readonly IClientImportTemplateBuilder _templateBuilder;

    public GetClientImportTemplateQueryHandler(
        ClientImportSynonymCatalog catalog,
        IClientImportTemplateBuilder templateBuilder,
        ILogger<GetClientImportTemplateQueryHandler> logger)
        : base(logger)
    {
        _catalog = catalog;
        _templateBuilder = templateBuilder;
    }

    public Task<ClientImportTemplateFile> Handle(GetClientImportTemplateQuery request, CancellationToken cancellationToken)
    {
        return ExecuteAsync(() =>
        {
            var language = ResolveLanguage(request);

            var content = _templateBuilder.Build(ClientImportTemplateLayout.SheetName, ClientImportTemplateLayout.Headers(_catalog, language));
            var fileName = string.Concat(ClientImportTemplateLayout.FileNamePrefix, language, ClientImportTemplateLayout.FileExtension);

            return Task.FromResult(new ClientImportTemplateFile(content, fileName, ClientImportTemplateLayout.ContentType));
        },
        "building client import template");
    }

    private string ResolveLanguage(GetClientImportTemplateQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            return SupportedLanguage(request.Language.Trim())
                ?? throw new ClientImportRejectedException(ClientImportErrorCodes.UnsupportedLanguage, $"The language '{request.Language}' is not supported.");
        }

        var preferred = (request.PreferredLanguages ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => SupportedLanguage(tag.Trim()) ?? SupportedLanguage(PrimarySubtag(tag.Trim())))
            .FirstOrDefault(language => language != null);

        return preferred ?? ClientImportTemplateLayout.DefaultLanguage;
    }

    private string? SupportedLanguage(string tag) =>
        _catalog.Languages.FirstOrDefault(l => string.Equals(l, tag, StringComparison.OrdinalIgnoreCase));

    private static string PrimarySubtag(string tag)
    {
        var separator = tag.IndexOf(LanguageSubtagSeparator);
        return separator > 0 ? tag[..separator] : tag;
    }
}
