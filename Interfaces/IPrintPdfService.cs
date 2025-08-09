using Klacks.Api.Presentation.Resources;

namespace Klacks.Api.Interfaces;

public interface IPrintPdfService
{
    HttpResultResource PrintAncenceGantt(int year, string lang);
}
