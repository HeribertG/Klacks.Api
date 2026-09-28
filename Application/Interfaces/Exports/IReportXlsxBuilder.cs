// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Reports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IReportXlsxBuilder
{
    ReportExportResult Build(ReportXlsxRequest request);
}
