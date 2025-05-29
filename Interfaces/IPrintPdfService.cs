using Klacks.Api.Resources;

namespace Klacks.Api.Interfaces;

public interface IPrintPdfService
{
    HttpResultResource PrintAncenceGantt(int year, string lang);
}
