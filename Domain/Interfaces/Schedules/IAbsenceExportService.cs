// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IAbsenceExportService
{
    HttpResultResource CreateExcelFile(string language);
}