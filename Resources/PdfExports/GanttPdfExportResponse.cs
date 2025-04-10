namespace Klacks.Api.Resources.PdfExports;

public class GanttPdfExportResponse
{
    public byte[] PdfContent { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; } = "application/pdf";
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }
}

