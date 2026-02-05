namespace Klacks.Api.Domain.Models.Reports;

public class BorderStyle
{
    public bool Left { get; set; }
    public bool Right { get; set; }
    public bool Top { get; set; }
    public bool Bottom { get; set; }
    public float Width { get; set; } = 1;
    public string Color { get; set; } = "#000000";
}
