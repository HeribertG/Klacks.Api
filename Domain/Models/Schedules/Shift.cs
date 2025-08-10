using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Models.Schedules;

public class Shift : BaseEntity
{
    // If the shift was cut by a parent shift after midnight
    public bool CuttingAfterMidnight { get; set; }

    public string Abbreviation { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid? MacroId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ShiftStatus Status { get; set; }

    #region Date and Time

    public TimeOnly AfterShift { get; set; }

    public TimeOnly BeforeShift { get; set; }

    public TimeOnly EndShift { get; set; }

    public DateOnly FromDate { get; set; }

    public TimeOnly StartShift { get; set; }

    public DateOnly? UntilDate { get; set; }

    public TimeOnly BriefingTime { get; set; }

    public TimeOnly DebriefingTime { get; set; }

    public TimeOnly TravelTimeAfter { get; set; }

    public TimeOnly TravelTimeBefore { get; set; }

    #endregion Date and Time

    #region WeekDay

    public bool IsFriday { get; set; }

    // True if the shift day is a holiday.
    public bool IsHoliday { get; set; }

    public bool IsMonday { get; set; }

    public bool IsSaturday { get; set; }

    public bool IsSunday { get; set; }

    public bool IsThursday { get; set; }

    public bool IsTuesday { get; set; }

    public bool IsWednesday { get; set; }

    // True if the shift day is any weekday or a holiday.
    public bool IsWeekdayOrHoliday { get; set; }

    #endregion WeekDay

    #region Time

    public bool IsSporadic { get; set; }

    public ShiftSporadic SporadicScope { get; set; }

    public bool IsTimeRange { get; set; }

    public int Quantity { get; set; }

    public int SumEmployees { get; set; }

    public decimal WorkTime { get; set; }
    
    #endregion Time

    #region Type

    public ShiftType ShiftType { get; set; }

    #endregion Type

    #region relation

    public Guid? OriginalId { get; set; } // All Shift are based on this original

    public Guid? ParentId { get; set; }

    public Guid? RootId { get; set; } // All Shift cuts are based on this root

    public int? Lft { get; set; }

    public int? Rgt { get; set; }

    #endregion relation

    #region Client

    public Guid? ClientId { get; set; }

    public Client? Client { get; set; }

    #endregion Client

    #region Groups

    public List<Group> Groups { get; set; } = new List<Group>();

    #endregion Groups
}