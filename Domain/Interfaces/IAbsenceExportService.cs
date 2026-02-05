using Klacks.Api.Application.DTOs;

namespace Klacks.Api.Domain.Interfaces;

public interface IAbsenceExportService
{
    HttpResultResource CreateExcelFile(string language);
}