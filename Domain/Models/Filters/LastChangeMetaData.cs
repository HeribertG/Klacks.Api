namespace Klacks.Api.Domain.Models.Filters;

public class LastChangeMetaData
{
    public DateTime LastChangesDate { get; set; }
    public string Author { get; set; } = string.Empty;
}