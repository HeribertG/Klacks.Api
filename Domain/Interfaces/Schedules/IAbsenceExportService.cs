// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.DTOs;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IAbsenceExportService
{
    HttpResultResource CreateExcelFile(string language);
}