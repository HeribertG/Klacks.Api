// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Common;

public static class DateRangeUtility
{   
    public static (DateTime startDate, DateTime endDate) GetYearRange(int year)
    {
        var startDate = new DateTime(year, 1, 1);
        var endDate = new DateTime(year + 1, 1, 1);
        return (startDate, endDate);
    }
        
    public static (DateTime startDate, DateTime endDate) GetMonthRange(int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);
        return (startDate, endDate);
    }
    
    public static (DateTime startDate, DateTime endDate) GetExtendedMonthRange(int year, int month, int daysBefore, int daysAfter)
    {
        var (startDate, endDate) = GetMonthRange(year, month);
        startDate = startDate.AddDays(-daysBefore);
        endDate = endDate.AddDays(-1).AddDays(daysAfter); 
        return (startDate, endDate);
    }
}