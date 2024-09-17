using Klacks_api.Resources;

namespace Klacks_api.Interfaces;

public interface IPrintPdfService
{
  HttpResultResource PrintAncenceGantt(int year, string lang);
}
