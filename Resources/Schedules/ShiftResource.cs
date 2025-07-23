using Klacks.Api.Enums;
using Klacks.Api.Resources.Associations;

namespace Klacks.Api.Resources.Schedules
{
    public class ShiftResource
    {
        // If the shift was cut by a parent shift after midnight
        public bool CuttingAfterMidnight { get; set; }

        public string Abbreviation { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public Guid Id { get; set; }

        public Guid MacroId { get; set; }

        public string Name { get; set; } = string.Empty;

        public Guid? ParentId { get; set; }

        // All Shift cuts are based on this root
        public Guid? RootId { get; set; }

        public int? Lft { get; set; }

        public int? Rgt { get; set; }

        public Guid? OriginalId { get; set; }

        public ShiftStatus Status { get; set; }

        #region Date and Time

        public TimeOnly AfterShift { get; set; }

        public TimeOnly BeforeShift { get; set; }

        public TimeOnly EndShift { get; set; }

        public DateOnly FromDate { get; set; }

        public TimeOnly StartShift { get; set; }

        public DateOnly? UntilDate { get; set; }

        public TimeOnly TravelTimeAfter { get; set; }

        public TimeOnly TravelTimeBefore { get; set; }

        public TimeOnly BriefingTime { get; set; }

        public TimeOnly DebriefingTime { get; set; }

        #endregion Date and Time

        #region WeekDay

        public bool IsFriday { get; set; }

        // Holiday, no matter what day of the week.
        public bool IsHoliday { get; set; }

        public bool IsMonday { get; set; }

        public bool IsSaturday { get; set; }

        public bool IsSunday { get; set; }

        public bool IsThursday { get; set; }

        public bool IsTuesday { get; set; }

        public bool IsWednesday { get; set; }

        // Holiday on a selected weekday..
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

        #region Client

        public Guid? ClientId { get; set; }

        public Staffs.ClientResource? Client { get; set; }

        #endregion Client

        #region Groups

        public List<SimpleGroupResource> Groups { get; set; } = new List<SimpleGroupResource>();

        #endregion Groups
    }
}