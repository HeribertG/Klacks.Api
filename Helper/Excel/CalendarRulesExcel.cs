using ClosedXML.Excel;
using Klacks.Api.Datas;
using Klacks.Api.Models.Settings;
using System.Reflection;

namespace Klacks.Api.Helper.Excel
{
  public class CalendarRulesExcel
  {
    private int countRows;
    private string fileName;

    private Dictionary<int, string> state = new() {
  { 1, "AG"},
  { 2, "AI" },
  { 3, "AR" },
  { 4, "BE" },
  { 5, "BL" },
  { 6, "BS" },
  { 7, "FR" },
  { 8, "GE" },
  { 9, "GL" },
  { 10, "GR" },
  { 11, "JU" },
  { 12, "LU" },
  { 13, "NE" },
  { 14, "NW" },
  { 15, "OW" },
  { 16, "SG" },
  { 17, "SH" },
  { 18, "SO" },
  { 19, "SZ" },
  { 20, "TG" },
  { 21, "TI" },
  { 22, "UR" },
  { 23, "VD" },
  { 24, "VS" },
  { 25, "ZG" },
  { 26, "ZH" },
  { 27, "CH" },
  { 28, "D" },
  { 29, "F" },
  { 30, "I" },
  { 31, "LI" },
  { 32, "USA" },
  { 33, "A" }
  };

    private IXLWorksheet worksheet;

    public CalendarRulesExcel(IQueryable<CalendarRule> data)
    {
      fileName = Path.GetTempFileName();
      fileName = fileName.Replace(".tmp", ".xlsx");
      countRows = data.Count() + 1;

      using (var workbook = new XLWorkbook())
      {
        worksheet = workbook.Worksheets.Add("Feiertagsregelliste");
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
      return 7;
    }

    private void CreateBody(IQueryable<CalendarRule> list)
    {
      int row = 1;
      foreach (CalendarRule item in list)
      {
        row += 1;
        int col = 1;

        SetStringCell(worksheet.Cell(row, col), item.Name != null ? item.Name.Fallback("De") : string.Empty);
        col += 1;
        SetStringCell(worksheet.Cell(row, col), item.Description != null ? item.Description.Fallback("De") : string.Empty);
        col += 1;
        SetStringCell(worksheet.Cell(row, col), "'" + item.Rule);
        col += 1;
        SetStringCell(worksheet.Cell(row, col), item.SubRule);
        col += 1;
        SetBooleanCell(worksheet.Cell(row, col), item.IsMandatory);
        col += 1;
        SetBooleanCell(worksheet.Cell(row, col), item.IsPaid);
        col += 1;
        SetStringCell(worksheet.Cell(row, col), item.State);
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
      SetHeaderCell(col, "Regel", worksheet);
      col += 1;
      SetHeaderCell(col, "Unterregel", worksheet);
      col += 1;
      SetHeaderCell(col, "Geltender Feiertag", worksheet);
      col += 1;
      SetHeaderCell(col, "Wird Vergütet", worksheet);
      col += 1;
      SetHeaderCell(col, "States", worksheet);
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
      // cell.DataType = XLDataType.Number;
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
      // cell.DataType = XLDataType.Number;
    }

    private void SetStringCell(IXLCell cell, string value)
    {
      cell.ShareString = true;
      cell.ShareString = true;
      cell.Value = value;
      // cell.DataType = XLDataType.Text;
    }
  }
}
