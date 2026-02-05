namespace Klacks.Api.Domain.Entities.Reports;

public class ReportField
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataBinding { get; set; } = string.Empty;
    public ReportFieldType Type { get; set; } = ReportFieldType.Text;
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 100;
    public float Height { get; set; } = 20;
    public FieldStyle Style { get; set; } = new();
    public string? Format { get; set; }
    public string? Formula { get; set; }
    public int SortOrder { get; set; }
}

public enum ReportFieldType
{
    Text = 0,
    Date = 1,
    Number = 2,
    Currency = 3,
    Boolean = 4,
    Formula = 5,
    Image = 6
}

public class FieldStyle
{
    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = 10;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public string TextColor { get; set; } = "#000000";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public BorderStyle Border { get; set; } = new();
}

public enum TextAlignment
{
    Left = 0,
    Center = 1,
    Right = 2,
    Justified = 3
}

public class BorderStyle
{
    public bool Left { get; set; }
    public bool Right { get; set; }
    public bool Top { get; set; }
    public bool Bottom { get; set; }
    public float Width { get; set; } = 1;
    public string Color { get; set; } = "#000000";
}
