// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders the deterministic sample dataset through the requested formatter, applying either the
/// supplied (not yet saved) patch or the stored override. Lets support verify a hotfix file before
/// enabling it for real exports; an invalid patch fails the preview with the validation errors.
/// </summary>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Application.Services.Exports;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Exports;

public class PreviewExportFormatOverrideQueryHandler : IRequestHandler<PreviewExportFormatOverrideQuery, ExportPreviewResult>
{
    private readonly IExportFormatFamilyResolver _familyResolver;
    private readonly IExportFormatOverrideRepository _repository;
    private readonly IEnumerable<IExportFormatter> _orderFormatters;
    private readonly IEnumerable<IPayrollExportFormatter> _payrollFormatters;
    private readonly IEnumerable<IClientPeriodExportFormatter> _clientPeriodFormatters;

    public PreviewExportFormatOverrideQueryHandler(
        IExportFormatFamilyResolver familyResolver,
        IExportFormatOverrideRepository repository,
        IEnumerable<IExportFormatter> orderFormatters,
        IEnumerable<IPayrollExportFormatter> payrollFormatters,
        IEnumerable<IClientPeriodExportFormatter> clientPeriodFormatters)
    {
        _familyResolver = familyResolver;
        _repository = repository;
        _orderFormatters = orderFormatters;
        _payrollFormatters = payrollFormatters;
        _clientPeriodFormatters = clientPeriodFormatters;
    }

    public async Task<ExportPreviewResult> Handle(PreviewExportFormatOverrideQuery request, CancellationToken cancellationToken)
    {
        if (!_familyResolver.TryResolve(request.FormatKey, out var family))
        {
            throw new InvalidRequestException($"Unknown export format: {request.FormatKey}");
        }

        var patchJson = request.PatchJson;
        if (patchJson is null)
        {
            var stored = await _repository.GetByFormatKeyAsync(request.FormatKey, cancellationToken);
            patchJson = stored is { IsEnabled: true } ? stored.PatchJson : null;
        }

        ExportFormatOverridePatch? patch = null;
        if (patchJson is not null)
        {
            var errors = ExportFormatOverridePatch.Validate(patchJson, family);
            if (errors.Count > 0)
            {
                throw new InvalidRequestException(string.Join(" ", errors));
            }

            patch = ExportFormatOverridePatch.Parse(patchJson);
        }

        var (content, extension, contentType) = Render(request.FormatKey, family, patch);

        return new ExportPreviewResult
        {
            FileContent = content,
            FileName = $"preview_{request.FormatKey}{extension}",
            ContentType = contentType,
            OverrideApplied = patch is not null,
        };
    }

    private (byte[] Content, string Extension, string ContentType) Render(string formatKey, string family, ExportFormatOverridePatch? patch)
    {
        if (family == ExportConstants.FormatFamilyPayroll)
        {
            var formatter = _payrollFormatters.First(f => f.FormatKey == formatKey);
            var config = ExportSampleDataFactory.CreatePayrollSampleConfig(formatKey);
            patch?.ApplyTo(config);
            var result = formatter.Format(ExportSampleDataFactory.CreatePayrollSample(), config);
            return (result.Content, formatter.FileExtension, formatter.ContentType);
        }

        var options = ExportSampleDataFactory.CreateSampleOptions();
        patch?.ApplyTo(options);

        if (family == ExportFormatFamilyResolver.FamilyClientPeriod)
        {
            var key = formatKey[ExportOverrideConstants.ClientPeriodFormatKeyPrefix.Length..];
            var formatter = _clientPeriodFormatters.First(f => f.FormatKey == key);
            return (formatter.Format(ExportSampleDataFactory.CreateClientPeriodSample(), options), formatter.FileExtension, formatter.ContentType);
        }

        var orderFormatter = _orderFormatters.First(f => f.FormatKey == formatKey);
        return (orderFormatter.Format(ExportSampleDataFactory.CreateOrderSample(), options), orderFormatter.FileExtension, orderFormatter.ContentType);
    }
}
