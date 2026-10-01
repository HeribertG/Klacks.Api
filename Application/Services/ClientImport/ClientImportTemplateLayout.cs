// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The columns of the downloadable import template, in order. The header of each is the first synonym
/// of its target in the requested language, so the detector recognises a filled-in template completely.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportTemplateLayout
{
    public const string FileNamePrefix = "klacks-employee-import-";
    public const string FileExtension = ".xlsx";
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string SheetName = "Import";
    public const string DefaultLanguage = "en";

    public static readonly IReadOnlyList<ClientImportTarget> Targets =
    [
        ClientImportTarget.FirstName,
        ClientImportTarget.LastName,
        ClientImportTarget.Title,
        ClientImportTarget.Gender,
        ClientImportTarget.Birthdate,
        ClientImportTarget.Street,
        ClientImportTarget.AddressLine2,
        ClientImportTarget.Zip,
        ClientImportTarget.City,
        ClientImportTarget.State,
        ClientImportTarget.Country,
        ClientImportTarget.Email,
        ClientImportTarget.Phone,
        ClientImportTarget.Mobile,
        ClientImportTarget.EntryDate,
        ClientImportTarget.ExitDate,
        ClientImportTarget.Contract,
        ClientImportTarget.Group,
        ClientImportTarget.Note
    ];

    public static List<string> Headers(ClientImportSynonymCatalog catalog, string language) =>
        Targets.Select(target => catalog.TemplateHeader(language, target)
            ?? throw new InvalidOperationException($"No template header for {target} in '{language}'."))
            .ToList();
}
