using Klacks.Api.Presentation.DTOs;

namespace Klacks.Api.Infrastructure.Interfaces;

public interface IPrintPdfService
{
    HttpResultResource PrintAncenceGantt(int year, string lang);
}
