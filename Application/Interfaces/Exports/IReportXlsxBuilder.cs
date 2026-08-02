// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Reports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IReportXlsxBuilder
{
    ReportExportResult Build(ReportXlsxRequest request);
}
