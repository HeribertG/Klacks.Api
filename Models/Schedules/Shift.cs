using Klacks.Api.Datas;
using Klacks.Api.Enums;

namespace Klacks.Api.Models.Schedules;

public class Shift : BaseEntity
{
  // If the shift was cut by a parent shift after midnight
  public bool CuttingAfterMidnight { get; set; }

  public string Description { get; set; } = string.Empty;

  public Guid MacroId { get; set; }

  public string Name { get; set; } = string.Empty;

  public Guid? ParentId { get; set; }

  // All Shift cuts are based on this root
  public Guid? RootId { get; set; }

  public ShiftStatus Status { get; set; }

  #region Date and Time

  public TimeOnly AfterShift { get; set; }

  public TimeOnly BeforeShift { get; set; }

  public TimeOnly EndShift { get; set; }

  public DateOnly FromDate { get; set; }

  public TimeOnly StartShift { get; set; }

  public DateOnly? UntilDate { get; set; }

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

  public bool IsTimeRange { get; set; }

  public int Quantity { get; set; }

  public decimal TravelTimeAfter { get; set; }

  public decimal TravelTimeBefore { get; set; }

  public decimal WorkTime { get; set; }

  #endregion Time

  #region Type

  public ShiftType ShiftType { get; set; }

  #endregion Type
}
