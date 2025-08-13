using Klacks.Api.Presentation.DTOs;

namespace Klacks.Api.Domain.Interfaces;

public interface IAbsenceExportService
{
    HttpResultResource CreateExcelFile(string language);
}