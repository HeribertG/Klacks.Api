using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class Break : ScheduleEntryBase
{
    public Guid AbsenceId { get; set; }

    public virtual Absence? Absence { get; set; }

    /// <summary>
    /// Multilingual description stored as JSONB in database.
    ///
    /// IMPORTANT: Unlike other MultiLanguage properties (e.g., Absence.Name, Shift.Description),
    /// this property is stored as a single JSONB column instead of separate columns per language
    /// (description_de, description_en, etc.).
    ///
    /// Reasons for JSONB storage:
    /// 1. MAINTAINABILITY: Adding new languages requires only updating the MultiLanguage class,
    ///    not creating migrations for new columns or updating SQL functions.
    /// 2. PERFORMANCE: For read-heavy operations (which is this use case), JSONB is equally fast
    ///    or faster than separate columns since the whole object is typically loaded.
    /// 3. SQL FUNCTION COMPATIBILITY: The get_schedule_entries() function can directly return
    ///    the JSONB column without manually building JSON from separate columns.
    ///
    /// Trade-off: Direct SQL filtering/indexing on individual languages is slightly less efficient,
    /// but this is acceptable since Description is rarely used in WHERE clauses.
    /// </summary>
    public MultiLanguage? Description { get; set; }
}
