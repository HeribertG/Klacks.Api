using ClosedXML.Excel;
using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Helper.Excel
{
  public class AbsenceExcel
  {
    private int countRows;
    private string fileName;
    private IXLWorksheet worksheet;

    public AbsenceExcel(IQueryable<Absence> data, string language)
    {
      fileName = Path.GetTempFileName();
      fileName = fileName.Replace(".tmp", ".xlsx");
      countRows = data.Count() + 1;

      using (var workbook = new XLWorkbook())
      {
        worksheet = workbook.Worksheets.Add("Adressenliste");
        CreateHeadline();
        CreateBody(data);

        SetAutofilter();
        SetColumnsWidth();

        workbook.SaveAs(fileName);
      }
    }

    public string FileName
    {
      get { return fileName; }
    }

    private int CountColumns()
    {
      return 10;
    }

    private void CreateBody(IQueryable<Absence> list)
    {
      int row = 1;
      foreach (Absence item in list)
      {
        row += 1;
        int col = 1;

        SetStringCell(worksheet.Cell(row, col), item.Name.Fallback("De"));
        col += 1;
        SetStringCell(worksheet.Cell(row, col), item.Description.Fallback("De"));
        col += 1;
        SetStringCell(worksheet.Cell(row, col), item.Color.ToString());
        col += 1;
        SetDoubleCell(worksheet.Cell(row, col), item.DefaultValue);
        col += 1;
        SetDoubleCell(worksheet.Cell(row, col), item.DefaultValue);
        col += 1;
        SetBooleanCell(worksheet.Cell(row, col), item.WithSaturday);
        col += 1;
        SetBooleanCell(worksheet.Cell(row, col), item.WithSunday);
        col += 1;
        SetBooleanCell(worksheet.Cell(row, col), item.WithHoliday);
        col += 1;
        SetBooleanCell(worksheet.Cell(row, col), item.HideInGantt);
      }
    }

    private void CreateHeadline()
    {
      worksheet.SheetView.FreezeRows(1);

      int col = 1;
      SetHeaderCell(col, "Name", worksheet);
      col += 1;
      SetHeaderCell(col, "Beschreibung", worksheet);
      col += 1;
      SetHeaderCell(col, "Farbe", worksheet);
      col += 1;
      SetHeaderCell(col, "Default Länge", worksheet);
      col += 1;
      SetHeaderCell(col, "Default Wert", worksheet);
      col += 1;
      SetHeaderCell(col, "Samstag wird berechnet", worksheet);
      col += 1;
      SetHeaderCell(col, "Sonntag wird berechnet", worksheet);
      col += 1;
      SetHeaderCell(col, "Feiertage werden berechnet", worksheet);
      col += 1;
      SetHeaderCell(col, "Nur für den internen Gebrauch", worksheet);
    }

    private void SetAutofilter()
    {
      int col = CountColumns();
      for (int i = 1; i < col; i++)
      {
        worksheet.RangeUsed().SetAutoFilter().Column(i);
      }
    }

    private void SetBooleanCell(IXLCell cell, bool value)
    {
      cell.Value = value;
      //cell.DataType = XLDataType.Boolean;
    }

    private void SetColumnsWidth()
    {
      int col = CountColumns() + 1;
      for (int i = 1; i < col; i++)
      {
        worksheet.Column(i).AdjustToContents(1, countRows);
      }
    }

    private void SetDoubleCell(IXLCell cell, double value)
    {
      cell.Value = "'" + value;
      //cell.DataType = XLDataType.Number;
    }

    private void SetHeaderCell(int col, string name, IXLWorksheet workSheet)
    {
      int row = 1;
      workSheet.Cell(row, col).Value = name;
      workSheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.Gray;
      workSheet.Cell(row, col).Style.Font.Bold = true;
      workSheet.Cell(row, col).Style.Font.FontColor = XLColor.White;
    }

    private void SetIntegerCell(IXLCell cell, int value)
    {
      cell.Value = "'" + value;
      //cell.DataType = XLDataType.Number;
    }

    private void SetStringCell(IXLCell cell, string value)
    {
      cell.ShareString = true;
      cell.ShareString = true;
      cell.Value = value;
      //cell.DataType = XLDataType.Text;
    }
  }
}
