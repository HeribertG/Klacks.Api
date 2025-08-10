using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Infrastructure.Interfaces;

public interface IGanttPdfExportService
{
    byte[] GeneratePdf(int year, string pageFormat, List<Client> clients, List<Absence> absences, string language);
}
