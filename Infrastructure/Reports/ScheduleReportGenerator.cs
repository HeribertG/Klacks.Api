using iTextSharp.text;
using iTextSharp.text.pdf;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Entities.Reports;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Reports;

public class ScheduleReportGenerator : IReportGenerator
{
    private readonly IClientRepository _clientRepository;
    private readonly IWorkRepository _workRepository;

    public ScheduleReportGenerator(
        IClientRepository clientRepository,
        IWorkRepository workRepository)
    {
        _clientRepository = clientRepository;
        _workRepository = workRepository;
    }

    public async Task<byte[]> GenerateScheduleReportAsync(
        Guid clientId,
        DateTime fromDate,
        DateTime toDate,
        ReportTemplate? template = null,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.Get(clientId);
        if (client is null)
        {
            throw new ArgumentException($"Client with ID {clientId} not found");
        }

        var works = await _workRepository.GetByClientAndDateRangeAsync(
            clientId, fromDate, toDate, cancellationToken);

        using var ms = new MemoryStream();
        var document = CreateDocument(template?.PageSetup);
        var writer = PdfWriter.GetInstance(document, ms);

        document.Open();

        // Add Header
        AddHeader(document, client, fromDate, toDate, template);

        // Add Schedule Table
        AddScheduleTable(document, works, template);

        // Add Footer
        AddFooter(document, template);

        document.Close();
        return ms.ToArray();
    }

    public Task<byte[]> GenerateClientReportAsync(
        Guid clientId,
        ReportTemplate? template = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Client report generation not yet implemented");
    }

    private static Document CreateDocument(ReportPageSetup? pageSetup)
    {
        var size = pageSetup?.Size switch
        {
            ReportPageSize.A3 => PageSize.A3.Rotate(),
            ReportPageSize.Letter => PageSize.Letter.Rotate(),
            _ => PageSize.A4.Rotate()
        };

        if (pageSetup?.Orientation == ReportOrientation.Portrait)
        {
            size = pageSetup.Size switch
            {
                ReportPageSize.A3 => PageSize.A3,
                ReportPageSize.Letter => PageSize.Letter,
                _ => PageSize.A4
            };
        }

        var margins = pageSetup?.Margins ?? new ReportMargins();
        return new Document(size, margins.Left, margins.Right, margins.Top, margins.Bottom);
    }

    private static void AddHeader(Document document, Domain.Models.Staffs.Client client, DateTime fromDate, DateTime toDate, ReportTemplate? template)
    {
        var headerSection = template?.Sections.FirstOrDefault(s => s.Type == ReportSectionType.Header);

        if (headerSection != null)
        {
            foreach (var field in headerSection.Fields.OrderBy(f => f.SortOrder))
            {
                var value = GetFieldValue(field, client, fromDate, toDate, null);
                var phrase = CreatePhrase(field, value);
                var paragraph = new Paragraph(phrase);
                paragraph.Alignment = GetAlignment(field.Style.Alignment);
                document.Add(paragraph);
            }
        }
        else
        {
            var nameFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14);
            var periodFont = FontFactory.GetFont(FontFactory.HELVETICA, 10);

            var fullName = $"{client.FirstName} {client.Name}";
            document.Add(new Paragraph(fullName, nameFont));
            document.Add(new Paragraph(
                $"Period: {fromDate:dd.MM.yyyy} - {toDate:dd.MM.yyyy}",
                periodFont));
            document.Add(new Paragraph(" "));
        }
    }

    private void AddScheduleTable(Document document, List<Work> works, ReportTemplate? template)
    {
        var detailSection = template?.Sections.FirstOrDefault(s => s.Type == ReportSectionType.Detail);

        if (detailSection != null && detailSection.Fields.Any())
        {
            AddCustomTable(document, works, detailSection);
        }
        else
        {
            AddDefaultTable(document, works);
        }
    }

    private static void AddDefaultTable(Document document, List<Work> works)
    {
        var table = new PdfPTable(4)
        {
            WidthPercentage = 100
        };
        table.SetWidths([25f, 30f, 30f, 15f]);

        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
        table.AddCell(CreateHeaderCell("Date", headerFont));
        table.AddCell(CreateHeaderCell("Time", headerFont));
        table.AddCell(CreateHeaderCell("Information", headerFont));
        table.AddCell(CreateHeaderCell("Hours", headerFont));

        var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9);
        var totalHours = 0m;

        foreach (var work in works.OrderBy(w => w.CurrentDate))
        {
            var dateStr = work.CurrentDate.ToString("ddd, dd.MM.yyyy");
            var timeStr = $"{work.StartTime:HH:mm} - {work.EndTime:HH:mm}";
            var info = work.Information ?? "-";
            var hours = (decimal)(work.EndTime - work.StartTime).TotalHours;

            table.AddCell(CreateDataCell(dateStr, dataFont));
            table.AddCell(CreateDataCell(timeStr, dataFont));
            table.AddCell(CreateDataCell(info, dataFont));
            table.AddCell(CreateDataCell(hours.ToString("0.00"), dataFont, Element.ALIGN_RIGHT));
            totalHours += hours;
        }

        var totalFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
        var totalCell = new PdfPCell(new Phrase("Total:", totalFont))
        {
            Colspan = 3,
            HorizontalAlignment = Element.ALIGN_RIGHT,
            Padding = 5,
            Border = Rectangle.TOP_BORDER
        };
        table.AddCell(totalCell);
        table.AddCell(new PdfPCell(new Phrase(totalHours.ToString("0.00"), totalFont))
        {
            HorizontalAlignment = Element.ALIGN_RIGHT,
            Padding = 5,
            Border = Rectangle.TOP_BORDER
        });

        document.Add(table);
    }

    private static void AddCustomTable(Document document, List<Work> works, ReportSection detailSection)
    {
        var fields = detailSection.Fields.OrderBy(f => f.SortOrder).ToList();
        if (!fields.Any()) return;

        var table = new PdfPTable(fields.Count)
        {
            WidthPercentage = 100
        };

        var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 10);
        foreach (var field in fields)
        {
            table.AddCell(CreateHeaderCell(field.Name, headerFont));
        }

        foreach (var work in works.OrderBy(w => w.CurrentDate))
        {
            foreach (var field in fields)
            {
                var value = GetFieldValue(field, null, default, default, work);
                var dataFont = CreateFont(field.Style);
                var alignment = GetAlignment(field.Style.Alignment);
                table.AddCell(CreateDataCell(value, dataFont, alignment));
            }
        }

        document.Add(table);
    }

    private static void AddFooter(Document document, ReportTemplate? template)
    {
        var footerSection = template?.Sections.FirstOrDefault(s => s.Type == ReportSectionType.Footer);

        if (footerSection != null)
        {
            document.Add(new Paragraph(" "));
            foreach (var field in footerSection.Fields.OrderBy(f => f.SortOrder))
            {
                var phrase = CreatePhrase(field, field.Formula ?? string.Empty);
                var paragraph = new Paragraph(phrase);
                paragraph.Alignment = GetAlignment(field.Style.Alignment);
                document.Add(paragraph);
            }
        }
    }

    private static string GetFieldValue(ReportField field, Domain.Models.Staffs.Client? client, DateTime fromDate, DateTime toDate, Work? work)
    {
        if (string.IsNullOrEmpty(field.DataBinding))
        {
            return field.Formula ?? string.Empty;
        }

        return field.DataBinding.ToLowerInvariant() switch
        {
            "client.fullname" => client != null ? $"{client.FirstName} {client.Name}" : string.Empty,
            "client.firstname" => client?.FirstName ?? string.Empty,
            "client.name" => client?.Name ?? string.Empty,
            "report.period" => $"{fromDate:dd.MM.yyyy} - {toDate:dd.MM.yyyy}",
            "report.date" => DateTime.Now.ToString("dd.MM.yyyy"),
            "work.date" => work?.CurrentDate.ToString("dd.MM.yyyy") ?? string.Empty,
            "work.day" => work?.CurrentDate.ToString("ddd") ?? string.Empty,
            "work.starttime" => work?.StartTime.ToString("HH:mm") ?? string.Empty,
            "work.endtime" => work?.EndTime.ToString("HH:mm") ?? string.Empty,
            "work.timerange" => work != null ? $"{work.StartTime:HH:mm} - {work.EndTime:HH:mm}" : string.Empty,
            "work.hours" => work != null ? ((decimal)(work.EndTime - work.StartTime).TotalHours).ToString("0.00") : "0.00",
            "work.information" => work?.Information ?? string.Empty,
            _ => field.Formula ?? string.Empty
        };
    }

    private static Phrase CreatePhrase(ReportField field, string value)
    {
        var font = CreateFont(field.Style);
        return new Phrase(value, font);
    }

    private static Font CreateFont(FieldStyle style)
    {
        var fontName = style.FontFamily switch
        {
            "Times New Roman" => FontFactory.TIMES,
            "Courier New" => FontFactory.COURIER,
            _ => FontFactory.HELVETICA
        };

        int styleFlags = Font.NORMAL;
        if (style.Bold) styleFlags |= Font.BOLD;
        if (style.Italic) styleFlags |= Font.ITALIC;
        if (style.Underline) styleFlags |= Font.UNDERLINE;

        return FontFactory.GetFont(fontName, style.FontSize, styleFlags);
    }

    private static int GetAlignment(TextAlignment alignment)
    {
        return alignment switch
        {
            TextAlignment.Center => Element.ALIGN_CENTER,
            TextAlignment.Right => Element.ALIGN_RIGHT,
            TextAlignment.Justified => Element.ALIGN_JUSTIFIED,
            _ => Element.ALIGN_LEFT
        };
    }

    private static PdfPCell CreateHeaderCell(string text, Font font)
    {
        return new PdfPCell(new Phrase(text, font))
        {
            BackgroundColor = new BaseColor(240, 240, 240),
            Padding = 5,
            BorderWidth = 0.5f
        };
    }

    private static PdfPCell CreateDataCell(string text, Font font, int alignment = Element.ALIGN_LEFT)
    {
        return new PdfPCell(new Phrase(text, font))
        {
            Padding = 5,
            BorderWidth = 0.5f,
            HorizontalAlignment = alignment
        };
    }
}
