// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the override catalog for the settings card: every override-capable format key with its
/// family, the whitelisted patch keys, and the stored override row (if any), plus the current app
/// version so the UI can flag overrides created under an older release.
/// </summary>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Exports;

public class ListExportFormatOverridesQueryHandler : IRequestHandler<ListExportFormatOverridesQuery, ExportFormatOverrideCatalog>
{
    private readonly IExportFormatFamilyResolver _familyResolver;
    private readonly IExportFormatOverrideRepository _repository;

    public ListExportFormatOverridesQueryHandler(
        IExportFormatFamilyResolver familyResolver,
        IExportFormatOverrideRepository repository)
    {
        _familyResolver = familyResolver;
        _repository = repository;
    }

    public async Task<ExportFormatOverrideCatalog> Handle(ListExportFormatOverridesQuery request, CancellationToken cancellationToken)
    {
        var overrides = (await _repository.GetAllAsync(cancellationToken)).ToDictionary(o => o.FormatKey);

        var formats = _familyResolver.GetAll()
            .OrderBy(f => f.Family)
            .ThenBy(f => f.FormatKey)
            .Select(f => new ExportFormatOverrideFormatInfo
            {
                FormatKey = f.FormatKey,
                Family = f.Family,
                AllowedKeys = ExportOverrideConstants.KeysForFamily(f.Family),
                Override = overrides.TryGetValue(f.FormatKey, out var entry) ? ToResource(entry) : null,
            })
            .ToList();

        return new ExportFormatOverrideCatalog
        {
            CurrentVersion = $"{MyVersion.Major}.{MyVersion.Minor}.{MyVersion.Patch}",
            Formats = formats,
        };
    }

    internal static ExportFormatOverrideResource ToResource(ExportFormatOverride entry) => new()
    {
        FormatKey = entry.FormatKey,
        PatchJson = entry.PatchJson,
        IsEnabled = entry.IsEnabled,
        Note = entry.Note,
        CreatedUnderVersion = entry.CreatedUnderVersion,
        UpdateTime = entry.UpdateTime ?? entry.CreateTime,
    };
}
