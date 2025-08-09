using Klacks.Api.Presentation.DTOs;

namespace Klacks.Api.Interfaces;

public interface IPrintPdfService
{
    HttpResultResource PrintAncenceGantt(int year, string lang);
}
