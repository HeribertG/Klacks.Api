using iTextSharp.text;
using iTextSharp.text.pdf;
using Klacks.Api.Infrastructure.Interfaces; // Ersetzen durch korrekten Namespace
using Klacks.Api.Models.Schedules; // Ersetzen durch korrekten Namespace
using Klacks.Api.Models.Staffs; // Ersetzen durch korrekten Namespace
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Klacks.Api.Services.Exports // Ersetzen durch korrekten Namespace
{
    public class GanttPdfExportService : IGanttPdfExportService
    {
        // Layout-Konstanten
        private const float MARGIN = 36f; // 36 Punkte = 0,5 Zoll Ränder
        private const float ROW_HEIGHT = 25f;
        private const float CLIENT_NAME_WIDTH = 150f;
        private const float MINIMUM_CELL_WIDTH = 2f;
        private const float TITLE_Y_OFFSET = 30f; // Abstand Titel zum Header
        // --- Header Höhen Aufteilung ---
        private const float HEADER_HEIGHT = 50f;        // Gesamthöhe Header
        private const float MONTH_AREA_HEIGHT = 20f;    // Höhe für Monatsnamen (jetzt OBERER Teil)
        private const float DAY_AREA_HEIGHT = 30f;      // Höhe für Tagesnummern (jetzt UNTERER Teil)
        // ---                                ---
        private const float CLIENT_TEXT_X_OFFSET = 5f;
        private const float ABSENCE_PADDING = 2f;
        private const float MINIMUM_ABSENCE_WIDTH_FOR_TEXT = 30f;

        // Styling-Konstanten
        private const float TITLE_FONT_SIZE = 16f;
        private const float HEADER_FONT_SIZE = 10f; // Für "Name" Überschrift
        private const float MONTH_FONT_SIZE = 12f;  // Für Monatsnamen
        private const float DAY_FONT_SIZE = 8f;     // Für Tagesnummern
        private const float CLIENT_FONT_SIZE = 9f;  // Für Client-Namen
        private const float ABSENCE_FONT_SIZE = 8f; // Für Abwesenheitstext
        private const float THICK_LINE_WIDTH = 1.0f;   // Für äußere Rahmen?
        private const float MEDIUM_LINE_WIDTH = 0.75f; // Für vertikale Monatslinien (volle Höhe)
        private const float THIN_LINE_WIDTH = 0.5f;    // Für die meisten inneren Linien
        private const int BRIGHTNESS_THRESHOLD = 128;  // Für Textkontrast

        // Farben
        private static readonly BaseColor NAME_BACKGROUND_COLOR = new BaseColor(240, 240, 240);
        private static readonly BaseColor EVEN_MONTH_COLOR = new BaseColor(200, 200, 240); // Farbe für gerade Monate
        private static readonly BaseColor ODD_MONTH_COLOR = new BaseColor(240, 200, 200);  // Farbe für ungerade Monate
        private static readonly BaseColor WEEKEND_COLOR = new BaseColor(245, 220, 220); // Rötlicher Ton für Wochenenden
        private static readonly BaseColor ALTERNATE_ROW_COLOR = new BaseColor(245, 245, 245); // Hintergrund für jede zweite Zeile
        private static readonly BaseColor DEFAULT_ABSENCE_COLOR = new BaseColor(100, 149, 237); // Standard-Blau für Abwesenheiten
        private static readonly BaseColor BLACK = BaseColor.Black;
        private static readonly BaseColor WHITE = BaseColor.White;
        private static readonly BaseColor DARK_GRAY = BaseColor.DarkGray;
        private static readonly BaseColor LIGHT_GRAY = BaseColor.LightGray;
        private static readonly BaseColor GRAY = BaseColor.Gray;

        // Private Felder für zustandsabhängige Variablen
        private float _cellWidth = MINIMUM_CELL_WIDTH;
        private Dictionary<int, float> _monthWidths = new Dictionary<int, float>();
        private Dictionary<int, float> _monthPositions = new Dictionary<int, float>();

        /// <summary>
        /// Generiert das Gantt-Diagramm als PDF.
        /// </summary>
        public byte[] GeneratePdf(int year, string pageFormat, List<Client> clients, List<Absence> absences, string language)
        {
            using var memoryStream = new MemoryStream();

            Rectangle pageSize = DeterminePageSize(pageFormat);
            var document = CreateDocument(pageSize);
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();
            AddMetadata(document);

            // Maximale Anzahl von Client-Zeilen pro Seite berechnen
            float availableHeightForRows = pageSize.Height - document.TopMargin - document.BottomMargin - HEADER_HEIGHT - TITLE_Y_OFFSET;
            int maxRowsPerPage = (int)Math.Floor(availableHeightForRows / ROW_HEIGHT);
            if (maxRowsPerPage < 1) maxRowsPerPage = 1; // Sicherstellen, dass mindestens eine Zeile passt

            DrawGanttChart(document, writer, year, clients, absences, pageFormat.ToLower() == "a4", language, maxRowsPerPage);

            document.Close();
            return memoryStream.ToArray();
        }

        private Rectangle DeterminePageSize(string pageFormat)
        {
            return pageFormat.ToLower() == "a3" ? PageSize.A3.Rotate() : PageSize.A4.Rotate();
        }

        private Document CreateDocument(Rectangle pageSize)
        {
            return new Document(pageSize, MARGIN, MARGIN, MARGIN, MARGIN);
        }

        private void AddMetadata(Document document)
        {
            document.AddTitle("Abwesenheits-Gantt-Diagramm");
            document.AddCreator("Klacks Abwesenheitsmanagement"); // Anpassen falls nötig
            document.AddCreationDate();
        }

        /// <summary>
        /// Hauptmethode zum Zeichnen des Gantt-Diagramms mit Paginierung.
        /// </summary>
        private void DrawGanttChart(Document document, PdfWriter writer, int year,
                                   List<Client> clients, List<Absence> absences, bool isA4, string language,
                                   int maxRowsPerPage)
        {
            var content = writer.DirectContent;
            float chartWidth = CalculateChartWidth(document);

            DateTime startDate = new DateTime(year, 1, 1);
            DateTime endDate = new DateTime(year, 12, 31);
            int daysInYear = CalculateDaysInYear(startDate, endDate);

            // Zellbreite basierend auf der Gesamtbreite und den Tagen des Jahres berechnen
            CalculateCellWidth(chartWidth, daysInYear);

            if (isA4)
            {
                // A4: Jahr horizontal aufteilen UND vertikal paginieren
                DrawOnA4FormatPaginated(document, content, year, clients, absences, startDate, endDate, language, maxRowsPerPage);
            }
            else
            {
                // A3: Nur vertikal paginieren
                DrawOnA3FormatPaginated(document, content, year, clients, absences, startDate, endDate, language, maxRowsPerPage);
            }
        }

        private float CalculateChartWidth(Document document)
        {
            return document.PageSize.Width - document.LeftMargin - document.RightMargin;
        }

        private int CalculateDaysInYear(DateTime startDate, DateTime endDate)
        {
            return (int)(endDate - startDate).TotalDays + 1;
        }

        private void CalculateCellWidth(float chartWidth, int daysInYear)
        {
            float availableWidth = chartWidth - CLIENT_NAME_WIDTH;
            _cellWidth = availableWidth / daysInYear;
            if (_cellWidth < MINIMUM_CELL_WIDTH) _cellWidth = MINIMUM_CELL_WIDTH;
        }

        /// <summary>
        /// Zeichnet das Diagramm im A3-Format mit vertikaler Paginierung.
        /// </summary>
        private void DrawOnA3FormatPaginated(Document document, PdfContentByte content, int year,
                                            List<Client> clients, List<Absence> absences,
                                            DateTime startDate, DateTime endDate, string language,
                                            int maxRowsPerPage)
        {
            float leftPosition = document.LeftMargin;
            float chartWidth = CalculateChartWidth(document);

            // Schleife über Client-Blöcke für Paginierung
            for (int i = 0; i < clients.Count; i += maxRowsPerPage)
            {
                if (i > 0) // Neue Seite für nachfolgende Blöcke
                {
                    document.NewPage();
                }

                // Aktuellen Client-Block für diese Seite bestimmen
                int count = Math.Min(maxRowsPerPage, clients.Count - i);
                List<Client> currentClientChunk = clients.Skip(i).Take(count).ToList();

                // Positionen für die aktuelle Seite
                float topPosition = document.PageSize.Height - document.TopMargin;

                // Header für diese Seite zeichnen (wichtig: mit Anzahl Clients DIESER Seite)
                DrawHeader(content, leftPosition, topPosition, chartWidth, year, startDate, endDate, currentClientChunk.Count);

                // Client-Zeilen für diesen Block zeichnen
                DrawClientRows(content, leftPosition, topPosition - HEADER_HEIGHT, currentClientChunk, absences, startDate, endDate, language);
            }
        }

        /// <summary>
        /// Zeichnet das Diagramm im A4-Format mit horizontaler Teilung UND vertikaler Paginierung.
        /// </summary>
        private void DrawOnA4FormatPaginated(Document document, PdfContentByte content, int year,
                                           List<Client> clients, List<Absence> absences,
                                           DateTime startDate, DateTime endDate, string language,
                                           int maxRowsPerPage)
        {
            float leftPosition = document.LeftMargin;
            float chartWidth = CalculateChartWidth(document);
            int daysInYear = CalculateDaysInYear(startDate, endDate);

            // Aufteilung des Jahres (kann angepasst werden, z.B. exakt 6 Monate)
            int daysFirstHalf = (int)Math.Ceiling(daysInYear / 2.0);
            DateTime firstHalfStartDate = startDate;
            DateTime firstHalfEndDate = startDate.AddDays(daysFirstHalf - 1);
            DateTime secondHalfStartDate = firstHalfEndDate.AddDays(1);
            DateTime secondHalfEndDate = endDate;

            // --- Erste Jahreshälfte ---
            bool firstPageOfFirstHalf = true;
            for (int i = 0; i < clients.Count; i += maxRowsPerPage)
            {
                if (!firstPageOfFirstHalf) { document.NewPage(); }
                firstPageOfFirstHalf = false;

                int count = Math.Min(maxRowsPerPage, clients.Count - i);
                List<Client> currentClientChunk = clients.Skip(i).Take(count).ToList();
                float topPosition = document.PageSize.Height - document.TopMargin;

                // Header für die ERSTE HÄLFTE zeichnen
                DrawHeader(content, leftPosition, topPosition, chartWidth, year, firstHalfStartDate, firstHalfEndDate, currentClientChunk.Count);
                // Client-Zeilen für die ERSTE HÄLFTE zeichnen
                DrawClientRows(content, leftPosition, topPosition - HEADER_HEIGHT, currentClientChunk, absences, firstHalfStartDate, firstHalfEndDate, language);
            }

            // --- Seitenumbruch vor der zweiten Hälfte (nur wenn Clients vorhanden sind) ---
            if (clients.Any())
            {
                document.NewPage();
            }

            // --- Zweite Jahreshälfte ---
            bool firstPageOfSecondHalf = true;
            for (int i = 0; i < clients.Count; i += maxRowsPerPage)
            {
                if (!firstPageOfSecondHalf) { document.NewPage(); }
                firstPageOfSecondHalf = false;

                int count = Math.Min(maxRowsPerPage, clients.Count - i);
                List<Client> currentClientChunk = clients.Skip(i).Take(count).ToList();
                float topPosition = document.PageSize.Height - document.TopMargin;

                // Header für die ZWEITE HÄLFTE zeichnen
                DrawHeader(content, leftPosition, topPosition, chartWidth, year, secondHalfStartDate, secondHalfEndDate, currentClientChunk.Count);
                // Client-Zeilen für die ZWEITE HÄLFTE zeichnen
                DrawClientRows(content, leftPosition, topPosition - HEADER_HEIGHT, currentClientChunk, absences, secondHalfStartDate, secondHalfEndDate, language);
            }
        }

        /// <summary>
        /// Zeichnet den gesamten Header-Bereich (Titel, Name, Monate, Tage, Linien).
        /// Reihenfolge: Monate oben, Tage unten.
        /// </summary>
        private void DrawHeader(PdfContentByte content, float x, float y, float width,
                               int year, DateTime startDate, DateTime endDate,
                               int clientCountOnPage)
        {
            float headerTopY = DrawTitle(content, x, y, year); // Y-Position nach dem Titel (Oberkante des Headers)

            // "Name" Spalten-Header (deckt weiterhin die gesamte Höhe ab)
            DrawNameHeader(content, x, headerTopY);

            // Monatspositionen berechnen (einmal pro Header-Zeichnung)
            CalculateMonthPositions(x, startDate, endDate);

            // 1. Monate zeichnen (OBERER Teil des Headers)
            DrawMonths(content, x, headerTopY, MONTH_AREA_HEIGHT, startDate, endDate, clientCountOnPage);

            // 2. Tage zeichnen (UNTERER Teil des Headers)
            float daysAreaTopY = headerTopY - MONTH_AREA_HEIGHT;
            DrawDays(content, x, daysAreaTopY, DAY_AREA_HEIGHT, startDate, endDate, clientCountOnPage);

            // 3. Horizontale und äußere Header-Rahmen zeichnen
            DrawHeaderBorders(content, x, headerTopY, MONTH_AREA_HEIGHT, startDate, endDate);

            // 4. Vertikale Monats-Trennlinien über die *gesamte* Höhe zeichnen (Header + Client-Zeilen dieser Seite)
            float clientRowsBottomY = headerTopY - HEADER_HEIGHT - (clientCountOnPage * ROW_HEIGHT);
            DrawVerticalMonthDividersFullHeight(content, headerTopY, clientRowsBottomY);
        }


        private float DrawTitle(PdfContentByte content, float x, float y, int year)
        {
            var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, TITLE_FONT_SIZE, BLACK);
            string title = "Abwesenheits-Gantt-Diagramm";
            ColumnText.ShowTextAligned(content, Element.ALIGN_LEFT, new Phrase($"{title} {year}", titleFont), x, y, 0);
            return y - TITLE_Y_OFFSET; // Y-Position für den Beginn des Headers zurückgeben
        }

        private void DrawNameHeader(PdfContentByte content, float x, float headerTopY)
        {
            float headerBottomY = headerTopY - HEADER_HEIGHT;
            // Hintergrund für Namensspalte im Header
            content.SetColorFill(NAME_BACKGROUND_COLOR);
            content.Rectangle(x, headerBottomY, CLIENT_NAME_WIDTH, HEADER_HEIGHT);
            content.Fill();

            // Rahmen um Namensspalten-Header
            content.SetColorStroke(DARK_GRAY);
            content.SetLineWidth(THIN_LINE_WIDTH);
            content.Rectangle(x, headerBottomY, CLIENT_NAME_WIDTH, HEADER_HEIGHT);
            content.Stroke();

            // "Name" Überschrift (vertikal zentriert im gesamten Header)
            var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, HEADER_FONT_SIZE, BLACK);
            float textY = headerBottomY + (HEADER_HEIGHT - headerFont.Size) / 2;
            ColumnText.ShowTextAligned(content, Element.ALIGN_CENTER, new Phrase("Name", headerFont), x + CLIENT_NAME_WIDTH / 2, textY, 0);
        }


        private void CalculateMonthPositions(float x, DateTime startDate, DateTime endDate)
        {
            _monthWidths.Clear();
            _monthPositions.Clear();

            float currentX = x + CLIENT_NAME_WIDTH;
            DateTime loopDate = startDate;

            while (loopDate <= endDate)
            {
                DateTime firstOfMonth = new DateTime(loopDate.Year, loopDate.Month, 1);
                DateTime nextMonth = firstOfMonth.AddMonths(1);
                int monthKey = loopDate.Month + loopDate.Year * 100; // Eindeutiger Key

                // Tage dieses Monats innerhalb des Zeitraums [startDate, endDate] zählen
                int daysInMonthInPeriod = 0;
                DateTime tempDate = (loopDate > startDate) ? loopDate : startDate; // Start am Monatsanfang oder startDate
                while (tempDate <= endDate && tempDate < nextMonth)
                {
                    daysInMonthInPeriod++;
                    tempDate = tempDate.AddDays(1);
                }


                if (daysInMonthInPeriod > 0)
                {
                    float monthWidth = daysInMonthInPeriod * _cellWidth;
                    if (!_monthPositions.ContainsKey(monthKey))
                    {
                        _monthPositions[monthKey] = currentX;
                    }
                    _monthWidths[monthKey] = monthWidth;
                    currentX += monthWidth; // Wichtig: currentX weiterschieben
                }


                // Zum nächsten relevanten Datum springen
                loopDate = (nextMonth <= endDate) ? nextMonth : endDate.AddDays(1);


            }
        }


        /// <summary>
        /// Zeichnet die Monatsnamen und Hintergründe im OBEREN Teil des Headers.
        /// </summary>
        private void DrawMonths(PdfContentByte content, float x, float monthAreaTopY, float monthAreaHeight,
                                DateTime startDate, DateTime endDate, int clientCountOnPage)
        {
            var cultureInfo = new CultureInfo("de-DE"); // Deutsche Monatsnamen

            // Monatspositionen sollten bereits in DrawHeader berechnet worden sein.

            foreach (var kvp in _monthPositions.OrderBy(p => p.Key)) // Nach Monat sortieren
            {
                int monthKey = kvp.Key;
                int month = monthKey % 100;

                if (_monthWidths.TryGetValue(monthKey, out float monthWidth) && monthWidth > 0)
                {
                    float monthStartX = kvp.Value;
                    // Hintergrund im oberen Bereich zeichnen
                    DrawMonthBackground(content, month, monthStartX, monthWidth, monthAreaTopY, monthAreaHeight);
                    // Namen im oberen Bereich zeichnen
                    DrawMonthName(content, month, monthStartX, monthWidth, monthAreaTopY, monthAreaHeight, cultureInfo);
                }
            }
        }

        private void DrawMonthBackground(PdfContentByte content, int month, float monthStartX, float monthWidth, float areaTopY, float areaHeight)
        {
            BaseColor monthColor = month % 2 != 0 ? ODD_MONTH_COLOR : EVEN_MONTH_COLOR; // Ungerade Monate zuerst (Januar)
            content.SetColorFill(monthColor);
            content.Rectangle(monthStartX, areaTopY - areaHeight, monthWidth, areaHeight); // Y-Position angepasst
            content.Fill();
        }

        private void DrawMonthName(PdfContentByte content, int month, float monthStartX, float monthWidth, float areaTopY, float areaHeight, CultureInfo cultureInfo)
        {
            string monthName = cultureInfo.DateTimeFormat.GetMonthName(month);
            var monthHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, MONTH_FONT_SIZE, BLACK);
            // Vertikal zentrieren im Monatsbereich (oberer Teil)
            float textY = areaTopY - areaHeight + (areaHeight - monthHeaderFont.Size) / 2;
            ColumnText.ShowTextAligned(content, Element.ALIGN_CENTER, new Phrase(monthName, monthHeaderFont), monthStartX + monthWidth / 2, textY, 0);
        }

        /// <summary>
        /// Zeichnet die Tagesnummern und Tages-Trennlinien im UNTEREN Teil des Headers.
        /// </summary>
        private void DrawDays(PdfContentByte content, float x, float dayAreaTopY, float dayAreaHeight,
                              DateTime startDate, DateTime endDate, int clientCountOnPage)
        {
            float currentX = x + CLIENT_NAME_WIDTH;
            DateTime currentDate = startDate;
            var dayFont = FontFactory.GetFont(FontFactory.HELVETICA, DAY_FONT_SIZE, BLACK);

            while (currentDate <= endDate)
            {
                // Tages-Trennlinie nur im unteren Header-Bereich
                DrawDayDivider(content, currentX, dayAreaTopY, dayAreaHeight);
                // Tagesnummer im unteren Header-Bereich
                DrawDayNumber(content, currentX, dayAreaTopY, dayAreaHeight, currentDate, dayFont);

                currentX += _cellWidth;
                currentDate = currentDate.AddDays(1);
            }
        }

        private void DrawDayDivider(PdfContentByte content, float x, float areaTopY, float areaHeight)
        {
            content.SetColorStroke(LIGHT_GRAY);
            content.SetLineWidth(THIN_LINE_WIDTH);
            content.MoveTo(x, areaTopY);
            content.LineTo(x, areaTopY - areaHeight); // Nur Höhe des Tagesbereichs
            content.Stroke();
        }

        private void DrawDayNumber(PdfContentByte content, float x, float areaTopY, float areaHeight,
                                   DateTime date, Font dayFont)
        {
            if (_cellWidth >= 10)
            {
                // Vertikal zentriert im Tagesbereich (unterer Teil)
                float textY = areaTopY - areaHeight + (areaHeight - dayFont.Size) / 2;
                ColumnText.ShowTextAligned(content, Element.ALIGN_CENTER, new Phrase(date.Day.ToString(), dayFont), x + _cellWidth / 2, textY, 0);
            }
        }

        /// <summary>
        /// Zeichnet die vertikalen Monats-Trennlinien über die volle Höhe (Header + ClientRows der Seite).
        /// </summary>
        private void DrawVerticalMonthDividersFullHeight(PdfContentByte content, float headerTopY, float clientRowsBottomY)
        {
            content.SetColorStroke(DARK_GRAY); // Etwas dunkler für bessere Sichtbarkeit
            content.SetLineWidth(MEDIUM_LINE_WIDTH);

            foreach (var kvp in _monthPositions.OrderBy(p => p.Key))
            {
                float monthStartX = kvp.Value;
                content.MoveTo(monthStartX, headerTopY); // Von ganz oben im Header
                content.LineTo(monthStartX, clientRowsBottomY); // Bis ganz unten bei den Clients
                content.Stroke();
            }

            // Letzte Linie rechts vom letzten Monat
            if (_monthPositions.Any())
            {
                var lastMonth = _monthPositions.OrderBy(p => p.Key).Last();
                if (_monthWidths.TryGetValue(lastMonth.Key, out float lastWidth))
                {
                    content.MoveTo(lastMonth.Value + lastWidth, headerTopY);
                    content.LineTo(lastMonth.Value + lastWidth, clientRowsBottomY);
                    content.Stroke();
                }
            }
            content.SetLineWidth(THIN_LINE_WIDTH); // Zurücksetzen auf Standard
        }


        /// <summary>
        /// Zeichnet die horizontalen Linien für den Header-Rahmen.
        /// </summary>
        private void DrawHeaderBorders(PdfContentByte content, float x, float headerTopY, float topPartHeight,
                                       DateTime startDate, DateTime endDate)
        {
            float calendarPartWidth = ((endDate - startDate).Days + 1) * _cellWidth;
            float totalHeaderWidth = CLIENT_NAME_WIDTH + calendarPartWidth;
            float headerBottomY = headerTopY - HEADER_HEIGHT;

            content.SetColorStroke(DARK_GRAY);
            content.SetLineWidth(THIN_LINE_WIDTH);

            // 1. Horizontale Linie ZWISCHEN Monate (oben) und Tage (unten)
            float middleLineY = headerTopY - topPartHeight; // Unterkante des oberen Teils
            content.MoveTo(x + CLIENT_NAME_WIDTH, middleLineY);
            content.LineTo(x + totalHeaderWidth, middleLineY);
            content.Stroke();

            // 2. Horizontale Linie GANZ UNTEN am Header (Trennlinie zu Client-Zeilen)
            content.MoveTo(x, headerBottomY); // Beginnt ganz links
            content.LineTo(x + totalHeaderWidth, headerBottomY);
            content.Stroke();

            // Vertikale Linie links vom Kalender wird von DrawNameHeader und DrawVerticalMonthDividersFullHeight gezeichnet
        }

        /// <summary>
        /// Zeichnet die Zeilen für einen Block von Clients auf der aktuellen Seite.
        /// </summary>
        private void DrawClientRows(PdfContentByte content, float x, float y,
                                    List<Client> clientsOnPage, List<Absence> allAbsences,
                                    DateTime pageStartDate, DateTime pageEndDate, string language)
        {
            float currentY = y; // Start Y-Position (direkt unter dem Header)
            float totalWidth = CLIENT_NAME_WIDTH + ((pageEndDate - pageStartDate).Days + 1) * _cellWidth;
            int clientCountOnThisPage = clientsOnPage.Count;

            if (clientCountOnThisPage == 0) return; // Nichts zu zeichnen

            // Äußeren Rahmen um alle Client-Zeilen DIESER Seite zeichnen
            DrawClientRowsBorder(content, x, currentY, totalWidth, clientCountOnThisPage);
            // Vertikale Trennlinie zwischen Namen und Kalender für die Zeilen DIESER Seite
            DrawClientNameDivider(content, x, currentY, clientCountOnThisPage);

            float rowContentX = x + CLIENT_NAME_WIDTH; // Start X für Kalenderinhalt

            // Zeichne Client-Zeilen für den übergebenen Chunk
            for (int i = 0; i < clientCountOnThisPage; i++)
            {
                Client client = clientsOnPage[i];
                float rowTopY = currentY;
                float rowBottomY = currentY - ROW_HEIGHT;

                // 1. Alternierenden Hintergrund zeichnen (ganze Breite)
                DrawAlternatingRowBackground(content, x, rowTopY, totalWidth, i);

                // 2. Wochenend-Hintergründe für diese Zeile zeichnen
                DrawWeekendBackgroundForRow(content, rowContentX, rowTopY, pageStartDate, pageEndDate);

                // 3. Client-Namen zeichnen
                DrawClientName(content, x, rowTopY, client);

                // 4. Abwesenheiten für diesen Client zeichnen
                DrawClientAbsences(content, x, rowTopY, client, allAbsences, pageStartDate, pageEndDate, language);

                // 5. Dünne vertikale Monatstrennlinien für diese Zeile zeichnen
                DrawMonthDividersInRow(content, rowTopY);

                // 6. Horizontale Trennlinie unter dieser Zeile
                DrawRowDivider(content, x, rowTopY, totalWidth);

                currentY -= ROW_HEIGHT; // Zur nächsten Zeilenposition gehen
            }
        }


        private void DrawClientRowsBorder(PdfContentByte content, float x, float topY, float width, int clientCountOnPage)
        {
            content.SetColorStroke(DARK_GRAY);
            content.SetLineWidth(THIN_LINE_WIDTH);
            content.Rectangle(x, topY - clientCountOnPage * ROW_HEIGHT, width, clientCountOnPage * ROW_HEIGHT);
            content.Stroke();
        }

        private void DrawClientNameDivider(PdfContentByte content, float x, float topY, int clientCountOnPage)
        {
            content.SetColorStroke(DARK_GRAY);
            content.SetLineWidth(THIN_LINE_WIDTH);
            content.MoveTo(x + CLIENT_NAME_WIDTH, topY);
            content.LineTo(x + CLIENT_NAME_WIDTH, topY - clientCountOnPage * ROW_HEIGHT);
            content.Stroke();
        }

        private void DrawAlternatingRowBackground(PdfContentByte content, float x, float rowTopY, float width, int rowIndex)
        {
            if (rowIndex % 2 != 0) // Zweite, vierte, etc. Zeile (Index 1, 3, ...)
            {
                content.SetColorFill(ALTERNATE_ROW_COLOR);
                content.Rectangle(x, rowTopY - ROW_HEIGHT, width, ROW_HEIGHT);
                content.Fill();
            }
        }

        /// <summary>
        /// Zeichnet den Wochenend-Hintergrund für eine einzelne Client-Zeile.
        /// </summary>
        private void DrawWeekendBackgroundForRow(PdfContentByte content, float rowContentStartX, float rowTopY, DateTime pageStartDate, DateTime pageEndDate)
        {
            DateTime currentDate = pageStartDate;
            float currentX = rowContentStartX;
            float rowBottomY = rowTopY - ROW_HEIGHT;

            content.SetColorFill(WEEKEND_COLOR); // Vorher speichern? Nein, hier setzen.

            while (currentDate <= pageEndDate)
            {
                if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday)
                {
                    // Zeichne Rechteck nur für diese Zelle
                    content.Rectangle(currentX, rowBottomY, _cellWidth, ROW_HEIGHT);
                    content.Fill(); // Fülle nur die Wochenendzelle
                }
                currentX += _cellWidth;
                currentDate = currentDate.AddDays(1);
            }
            // Farbe muss nicht zurückgesetzt werden, da sie bei Bedarf überschrieben wird
        }


        private void DrawClientName(PdfContentByte content, float x, float rowTopY, Client client)
        {
            var clientFont = FontFactory.GetFont(FontFactory.HELVETICA, CLIENT_FONT_SIZE, BLACK);
            string clientName = !string.IsNullOrEmpty(client.Company) ? client.Company : $"{client.FirstName} {client.Name}";

            // Vertikal in der Zeile zentrieren
            float textY = rowTopY - ROW_HEIGHT + (ROW_HEIGHT - clientFont.Size) / 2; // Vertikal zentriert
            string truncatedName = TruncateText(clientName, clientFont, CLIENT_NAME_WIDTH - CLIENT_TEXT_X_OFFSET * 2);

            ColumnText.ShowTextAligned(content, Element.ALIGN_LEFT, new Phrase(truncatedName, clientFont), x + CLIENT_TEXT_X_OFFSET, textY, 0);
        }

        private string TruncateText(string text, Font font, float maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0) return string.Empty;

            var baseFont = font.GetCalculatedBaseFont(true); // Unicode beachten
            float textWidth = baseFont.GetWidthPoint(text, font.Size);

            if (textWidth <= maxWidth) return text;


            string ellipsis = "...";
            float ellipsisWidth = baseFont.GetWidthPoint(ellipsis, font.Size);
            if (maxWidth < ellipsisWidth) return string.Empty; // Nicht mal "..." passt


            string truncatedText = text;
            // Von hinten kürzen, bis Text + Ellipsis passen
            while (truncatedText.Length > 0 && baseFont.GetWidthPoint(truncatedText + ellipsis, font.Size) > maxWidth)
            {
                truncatedText = truncatedText.Substring(0, truncatedText.Length - 1);
            }

            // Fallback, falls selbst ein Zeichen + Ellipse zu breit ist (unwahrscheinlich)
            if (truncatedText.Length == 0 && baseFont.GetWidthPoint(ellipsis, font.Size) > maxWidth)
            {
                // Versuche nur den ersten Buchstaben
                if (text.Length > 0)
                {
                    string firstChar = text.Substring(0, 1);
                    if (baseFont.GetWidthPoint(firstChar, font.Size) <= maxWidth) return firstChar;
                }
                return string.Empty; // Nichts passt
            }


            return truncatedText + ellipsis;
        }


        private void DrawClientAbsences(PdfContentByte content, float x, float rowTopY,
                                        Client client, List<Absence> allAbsences,
                                        DateTime pageStartDate, DateTime pageEndDate, string language)
        {
            foreach (var breakItem in client.Breaks.OrderBy(b => b.From)) // Sortieren für konsistente Überlappung
            {
                // Nur Breaks berücksichtigen, die sich mit dem Zeitraum DIESER Seite überschneiden
                DateTime breakActualStart = breakItem.From;
                DateTime breakActualEnd = breakItem.Until;

                // Finde den tatsächlichen Überlappungsbereich mit der aktuellen Seite
                DateTime drawStart = (breakActualStart > pageStartDate) ? breakActualStart : pageStartDate;
                DateTime drawEnd = (breakActualEnd < pageEndDate) ? breakActualEnd : pageEndDate;

                if (drawStart <= drawEnd) // Nur wenn es eine Überschneidung gibt
                {
                    var absence = allAbsences.FirstOrDefault(a => a.Id == breakItem.AbsenceId);
                    if (absence != null)
                    {
                        DrawSingleAbsence(content, x, rowTopY, breakItem, absence, drawStart, drawEnd, pageStartDate, language);
                    }
                }
            }
        }

        private void DrawSingleAbsence(PdfContentByte content, float rowX, float rowTopY,
                                       Break breakItem, Absence absence,
                                       DateTime drawStart, DateTime drawEnd, DateTime pageStartDate, string language)
        {
            // Offset relativ zum Start der *aktuellen Seite* (pageStartDate)
            float breakStartOffsetDays = (float)(drawStart - pageStartDate).TotalDays;
            // Dauer des sichtbaren Teils
            float breakVisibleDays = (float)(drawEnd - drawStart).TotalDays + 1;

            float absenceX = rowX + CLIENT_NAME_WIDTH + (breakStartOffsetDays * _cellWidth);
            float absenceWidth = breakVisibleDays * _cellWidth;

            // Zeichnen
            BaseColor absenceColor = GetAbsenceColor(absence.Color);
            float blockY = rowTopY - ROW_HEIGHT + ABSENCE_PADDING;
            float blockHeight = ROW_HEIGHT - 2 * ABSENCE_PADDING;

            DrawAbsenceBlock(content, absenceX, blockY, absenceWidth, blockHeight, absenceColor);
            DrawAbsenceText(content, absenceX, blockY + blockHeight / 2, absenceWidth, absence, absenceColor, language); // Y-Koord. ist Mitte des Blocks
        }


        private BaseColor GetAbsenceColor(string colorHex)
        {
            if (!string.IsNullOrEmpty(colorHex))
            {
                string hex = colorHex.TrimStart('#');
                if (hex.Length == 6)
                {
                    try
                    {
                        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
                        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
                        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
                        return new BaseColor(r, g, b);
                    }
                    catch { /* Fehler ignorieren, Fallback nutzen */ }
                }
            }
            return DEFAULT_ABSENCE_COLOR;
        }

        private void DrawAbsenceBlock(PdfContentByte content, float x, float y, float width, float height, BaseColor color)
        {
            content.SetColorFill(color);
            content.Rectangle(x, y, width, height);
            content.Fill();

            content.SetColorStroke(DARK_GRAY);
            content.SetLineWidth(THIN_LINE_WIDTH);
            content.Rectangle(x, y, width, height);
            content.Stroke();
        }

        private void DrawAbsenceText(PdfContentByte content, float blockX, float blockCenterY, // Y ist jetzt die Mitte des Blocks
                                     float blockWidth, Absence absence, BaseColor backgroundColor, string language)
        {
            if (blockWidth >= MINIMUM_ABSENCE_WIDTH_FOR_TEXT)
            {
                string absenceName = GetLocalizedAbsenceName(absence, language);
                BaseColor textColor = IsColorDark(backgroundColor) ? WHITE : BLACK;
                var absenceFont = FontFactory.GetFont(FontFactory.HELVETICA, ABSENCE_FONT_SIZE, textColor);

                string textToDraw = TruncateText(absenceName, absenceFont, blockWidth - 4f); // Kleiner Puffer

                // Text vertikal zentrieren im Block
                float textY = blockCenterY - (absenceFont.Size / 2) + 1; // Leichte Anpassung nach oben

                ColumnText.ShowTextAligned(content, Element.ALIGN_CENTER, new Phrase(textToDraw, absenceFont), blockX + blockWidth / 2, textY, 0);
            }
        }

        private string GetLocalizedAbsenceName(Absence absence, string language)
        {
            // Annahme: absence.Name ist ein Objekt mit Properties De, En, Fr, It
            // Passe dies an deine tatsächliche Struktur an
            try
            {
                string name = language.ToLower() switch
                {
                    "de" => absence.Name?.De,
                    "en" => absence.Name?.En,
                    "fr" => absence.Name?.Fr,
                    "it" => absence.Name?.It,
                    _ => absence.Name?.De // Fallback
                };
                return !string.IsNullOrEmpty(name) ? name : absence.Id.ToString(); // Fallback auf ID wenn Name leer/null
            }
            catch
            {
                return absence.Id.ToString(); // Fallback auf ID bei Fehler
            }
        }

        /// <summary>
        /// Zeichnet die dünnen vertikalen Monatstrennlinien innerhalb einer Client-Zeile.
        /// </summary>
        private void DrawMonthDividersInRow(PdfContentByte content, float rowTopY)
        {
            content.SetColorStroke(LIGHT_GRAY); // Sehr helle Linien innerhalb der Zeilen
            content.SetLineWidth(THIN_LINE_WIDTH);

            foreach (var kvp in _monthPositions.OrderBy(p => p.Key))
            {
                float monthStartX = kvp.Value;
                content.MoveTo(monthStartX, rowTopY);
                content.LineTo(monthStartX, rowTopY - ROW_HEIGHT); // Nur Höhe einer Zeile
                content.Stroke();
            }
            // Letzte Linie rechts vom letzten Monat
            if (_monthPositions.Any())
            {
                var lastMonth = _monthPositions.OrderBy(p => p.Key).Last();
                if (_monthWidths.TryGetValue(lastMonth.Key, out float lastWidth))
                {
                    content.MoveTo(lastMonth.Value + lastWidth, rowTopY);
                    content.LineTo(lastMonth.Value + lastWidth, rowTopY - ROW_HEIGHT);
                    content.Stroke();
                }
            }
        }


        private void DrawRowDivider(PdfContentByte content, float x, float rowTopY, float width)
        {
            content.SetColorStroke(LIGHT_GRAY);
            content.SetLineWidth(THIN_LINE_WIDTH);
            content.MoveTo(x, rowTopY - ROW_HEIGHT);
            content.LineTo(x + width, rowTopY - ROW_HEIGHT);
            content.Stroke();
        }

        private bool IsColorDark(BaseColor color)
        {
            float brightness = (color.R * 0.299f + color.G * 0.587f + color.B * 0.114f);
            return brightness < BRIGHTNESS_THRESHOLD;
        }
    }


    // --- BEGINN PLATZHALTER-MODELLE ---
    //       ERSETZE DIESE DURCH DEINE ECHTEN KLASSEN/NAMESPACES

    namespace Klacks.Api.Models.Staffs // Beispiel-Namespace
    {
        public class Client
        {
            public int Id { get; set; } // Annahme
            public string FirstName { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Company { get; set; } = string.Empty;
            public List<Break> Breaks { get; set; } = new List<Break>();
        }

        public class Break
        {
            public DateTime From { get; set; }
            public DateTime Until { get; set; }
            public int AbsenceId { get; set; }
            // Weitere Properties?
        }
    }

    namespace Klacks.Api.Models.Schedules // Beispiel-Namespace
    {
        public class Absence
        {
            public int Id { get; set; }
            public LocalizedName? Name { get; set; } // Nullable machen
            public string? Color { get; set; } // Nullable machen, Hex-String wie "#RRGGBB" oder "RRGGBB"
                                               // Weitere Properties?
        }

        // Beispiel für eine Klasse für lokalisierte Namen
        public class LocalizedName
        {
            public string? De { get; set; }
            public string? En { get; set; }
            public string? Fr { get; set; }
            public string? It { get; set; }
        }
    }

    namespace Klacks.Api.Interfaces // Beispiel-Namespace
    {
        public interface IGanttPdfExportService
        {
            byte[] GeneratePdf(int year, string pageFormat, List<Client> clients, List<Absence> absences, string language);
        }
    }
    // --- ENDE PLATZHALTER-MODELLE ---

} // Ende Namespace Klacks.Api.Services.Exports