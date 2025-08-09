namespace Klacks.Api.Presentation.DTOs.PdfExports;

public class GanttPdfExportResponse
{
    public byte[] PdfContent { get; set; } = [];

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = "application/pdf";

    public bool Success { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;
}
