namespace Klacks.Api.Extensions;

public static class DateOnlyExtensions
{
    public static DateTime ToDateTime(this DateOnly dateOnly)
    {
        return dateOnly.ToDateTime(TimeOnly.MinValue);
    }

    public static DateTime? ToDateTime(this DateOnly? dateOnly)
    {
        return dateOnly?.ToDateTime(TimeOnly.MinValue);
    }
}
