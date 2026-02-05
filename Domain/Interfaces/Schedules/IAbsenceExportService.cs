using Klacks.Api.Application.DTOs;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IAbsenceExportService
{
    HttpResultResource CreateExcelFile(string language);
}