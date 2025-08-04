using Klacks.Api.Enums;
using Klacks.Api.Interfaces.Domains;
using Klacks.Api.Models.Settings;

namespace Klacks.Api.Services.Holidays;

public class HolidaysListCalculator : IHolidaysListCalculator
{
    #region Constants
    private const string WEEKDAY_NAME = "SUMOTUWETHFRSA"; // Sunday (SU),Monday (MO),Tuesday (TU),Wednesday (WE),Thursday (TH),Friday (FR),Saturday (SA)
    private const string EASTER_STRING = "EASTER";
    private const int MONTH_START_INDEX = 0;
    private const int DAY_START_INDEX = 3;
    #endregion

    #region Properties
    public int CurrentYear { get; set; } = DateTime.Now.Year;

    public List<CalendarRule> Rules { get; private set; } = new List<CalendarRule>();

    public List<HolidayDate> HolidayList { get; private set; } = new List<HolidayDate>();
    #endregion

    #region Rule Management
    public void Add(CalendarRule rule)
    {
        Rules.Add(rule);
    }

    public void AddRange(IEnumerable<CalendarRule> rules)
    {
        Rules.AddRange(rules);
    }

    public void Remove(int index)
    {
        if (index >= 0 && index < Rules.Count)
        {
            Rules.RemoveAt(index);
        }
    }

    public CalendarRule? GetRule(int index)
    {
        return index >= 0 && index < Rules.Count ? Rules[index] : null;
    }

    public int Count => Rules.Count;

    public void Clear()
    {
        Rules.Clear();
        HolidayList.Clear();
    }
    #endregion

    #region Holiday Computation
    public void ComputeHolidays()
    {
        HolidayList.Clear();

        if (Rules.Count == 0)
        {
            return;
        }

        var easterDate = CalculateEaster(CurrentYear);

        foreach (var rule in Rules)
        {
            var holiday = new HolidayDate
            {
                CurrentName = rule.Name?.En?? "",
                CurrentDate = ConvertDate(easterDate, CurrentYear, rule.Rule),
                Officially = rule.IsMandatory
            };

            if (!string.IsNullOrEmpty(rule.SubRule))
            {
                ApplySubRules(rule.SubRule, holiday);
            }

            holiday.FormatDate = FormatDate(holiday.CurrentDate);
            HolidayList.Add(holiday);
        }

        HolidayList.Sort((a, b) => a.CurrentDate.CompareTo(b.CurrentDate));
    }

    private void ApplySubRules(string subRules, HolidayDate holiday)
    {
        var rules = subRules.Split(';');

        foreach (var rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule) || rule.Length < 4) // SA+1 = minimum 4 characters
            {
                continue;
            }

            // Get weekday from DayOfWeek enum (Sunday = 0, Monday = 1, etc.)
            var currentWeekday = (int)holiday.CurrentDate.DayOfWeek;
            
            // Parse target weekday from rule (e.g., "SA" for Saturday)
            var targetWeekdayCode = rule.Substring(0, 2).ToUpper();
            var targetWeekdayIndex = WEEKDAY_NAME.IndexOf(targetWeekdayCode);
            if (targetWeekdayIndex == -1)
            {
                continue;
            }

            var targetWeekday = targetWeekdayIndex / 2; // Each weekday code is 2 chars

            // Parse operation and offset
            var operation = rule.Substring(2, 1);
            if (!int.TryParse(rule.Substring(3), out int dayOffset) || dayOffset == 0)
            {
                continue;
            }

            // Apply rule only if current day matches target weekday
            if (currentWeekday == targetWeekday)
            {
                if (operation == "+")
                {
                    holiday.CurrentDate = holiday.CurrentDate.AddDays(dayOffset);
                }
                else if (operation == "-")
                {
                    holiday.CurrentDate = holiday.CurrentDate.AddDays(-dayOffset);
                }
                break; // Apply only the first matching rule
            }
        }
    }
    #endregion

    #region Easter Calculation
    public DateOnly CalculateEaster(int year)
    {
        // Gaussian Easter formula
        var goldenNumber = year % 19;
        var century = year / 100;
        var epactOffset = ((8 * century) + 13) / 25;
        var leapCenturyCorrection = century / 4;
        var fixedConstantM = (15 - epactOffset + century - leapCenturyCorrection) % 30;
        var fixedConstantN = (4 + century - leapCenturyCorrection) % 7;
        var moonParameter = ((19 * goldenNumber) + fixedConstantM) % 30;
        var weekParameter = ((2 * (year % 4)) + (4 * (year % 7)) + (6 * moonParameter) + fixedConstantN) % 7;

        var day = 22 + moonParameter + weekParameter;
        var month = 3; // March

        if (day > 31)
        {
            day = moonParameter + weekParameter - 9;
            month = 4; // April
        }

        // Special rules for certain years
        if (day == 26)
        {
            day = 19;
        }
        else if (day == 25 && moonParameter == 28 && weekParameter == 6 && goldenNumber > 10)
        {
            day = 18;
        }

        return new DateOnly(year, month, day);
    }
    #endregion

    #region Date Conversion
    private DateOnly ConvertDate(DateOnly easterDate, int year, string ruleString)
    {
        DateOnly calcDate;

        if (ruleString.StartsWith(EASTER_STRING))
        {
            calcDate = CalculateEasterRelatedDate(easterDate, ruleString);
        }
        else
        {
            var month = int.Parse(ruleString.Substring(MONTH_START_INDEX, 2)); // 2 characters for month
            var day = int.Parse(ruleString.Substring(DAY_START_INDEX, 2)); // 2 characters for day
            calcDate = new DateOnly(year, month, day);

            if (ruleString.Length > 5)
            {
                var dayOfWeek = CalculateDayOfWeek(ruleString);
                var alternativeDayOfWeek = (int)calcDate.DayOfWeek + 1;
                var ruleToken = ruleString.Substring(8, 1);

                if (dayOfWeek != alternativeDayOfWeek)
                {
                    calcDate = ConstructDate(calcDate, ruleToken, dayOfWeek, alternativeDayOfWeek);
                }

                var dayOffset = int.Parse(ruleString.Substring(5, 3));
                calcDate = calcDate.AddDays(dayOffset);
            }
        }

        return calcDate;
    }

    private int CalculateDayOfWeek(string ruleString)
    {
        var weekDayName = ruleString.Substring(9, 2);
        return (WEEKDAY_NAME.IndexOf(weekDayName) / 2) + 1;
    }

    private DateOnly ConstructDate(DateOnly calcDate, string ruleToken, int dayOfWeek, int alternativeDayOfWeek)
    {
        int difference;

        if (ruleToken == "+" || ruleToken == "&")
        {
            difference = dayOfWeek - alternativeDayOfWeek;
            calcDate = calcDate.AddDays(difference);

            if (difference < 0)
            {
                calcDate = calcDate.AddDays(7);
            }

            if (ruleToken == "&")
            {
                calcDate = calcDate.AddDays(-7);
            }
        }
        else
        {
            difference = alternativeDayOfWeek - dayOfWeek;
            if (difference < 0)
            {
                calcDate = calcDate.AddDays(-difference + 7);
            }
            else
            {
                calcDate = calcDate.AddDays(-difference);
            }
        }

        return calcDate;
    }

    private DateOnly CalculateEasterRelatedDate(DateOnly easterDate, string ruleString)
    {
        DateOnly calcDate = easterDate;

        // Get the part after "EASTER"
        var offsetString = ruleString.Substring(EASTER_STRING.Length);
        
        if (!string.IsNullOrEmpty(offsetString))
        {
            // Check for +/- sign and parse the number
            if (offsetString.StartsWith("+") || offsetString.StartsWith("-"))
            {
                var sign = offsetString.StartsWith("+") ? 1 : -1;
                var remainingString = offsetString.Substring(1);
                
                // Find where the number ends (could be followed by weekday info)
                var numberPart = "";
                var weekdayPart = "";
                
                for (int i = 0; i < remainingString.Length; i++)
                {
                    if (char.IsDigit(remainingString[i]))
                    {
                        numberPart += remainingString[i];
                    }
                    else
                    {
                        weekdayPart = remainingString.Substring(i);
                        break;
                    }
                }
                
                if (int.TryParse(numberPart, out int dayOffset))
                {
                    calcDate = easterDate.AddDays(sign * dayOffset);
                }
                
                // Handle weekday part if present
                if (!string.IsNullOrEmpty(weekdayPart) && weekdayPart.Length >= 3)
                {
                    var weekdaySign = weekdayPart.Substring(0, 1);
                    var weekdayName = weekdayPart.Substring(1, 2);
                    var dayOfWeek = (WEEKDAY_NAME.IndexOf(weekdayName) / 2) + 1;
                    var alternativeDayOfWeek = (int)calcDate.DayOfWeek + 1;

                    int differenceDays;
                    if (weekdaySign == "+")
                    {
                        differenceDays = dayOfWeek - alternativeDayOfWeek + 7;
                    }
                    else
                    {
                        differenceDays = dayOfWeek - alternativeDayOfWeek - 7;
                    }

                    calcDate = calcDate.AddDays(differenceDays);
                }
            }
        }

        return calcDate;
    }
    #endregion

    #region Utility Methods
    public int GetIso8601WeekNumber(DateOnly inputDate)
    {
        var date = inputDate.ToDateTime(TimeOnly.MinValue);
        var thursday = date.AddDays(4 - ((int)date.DayOfWeek == 0 ? 7 : (int)date.DayOfWeek));
        var jan1 = new DateTime(thursday.Year, 1, 1);

        return (int)Math.Ceiling((thursday - jan1).TotalDays / 7.0);
    }

    public string FormatDate(DateOnly date)
    {
        return date.ToString("ddd dd.MMM.yyyy");
    }

    public HolidayStatus IsHoliday(DateOnly currentDate)
    {
        if (HolidayList.Count == 0)
        {
            return HolidayStatus.NotAHoliday;
        }

        var holiday = HolidayList.FirstOrDefault(x => x.CurrentDate == currentDate);

        return holiday?.Officially == true
            ? HolidayStatus.OfficialHoliday
            : holiday != null
                ? HolidayStatus.UnofficialHoliday
                : HolidayStatus.NotAHoliday;
    }

    public HolidayDate? GetHolidayInfo(DateOnly currentDate)
    {
        return HolidayList.FirstOrDefault(x => x.CurrentDate == currentDate);
    }

    public int GetDayOfYear(DateOnly date)
    {
        return date.DayOfYear;
    }

    public int GetTotalDaysInCurrentYear()
    {
        return IsLeapYear(CurrentYear) ? 366 : 365;
    }

    public int GetDaysInMonth(int month, int year)
    {
        return month switch
        {
            4 or 6 or 9 or 11 => 30,
            1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
            2 => IsLeapYear(year) ? 29 : 28,
            _ => throw new ArgumentException("Invalid month", nameof(month))
        };
    }

    public bool IsLeapYear(int year)
    {
        return (year % 4 == 0 && year % 100 != 0) || year % 400 == 0;
    }
    #endregion
}
