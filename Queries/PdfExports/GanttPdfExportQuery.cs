using Klacks.Api.Presentation.Resources.PdfExports;
using MediatR;

namespace Klacks.Api.Queries.PdfExports;

public record GanttPdfExportQuery : IRequest<GanttPdfExportResponse>
{
    public int Year { get; init; }
    public string PageFormat { get; init; } = "A3"; // "A3" oder "A4"
    public string Language { get; init; } = "de";
}
