namespace Klacks.Api.Domain.Models.Schedules;

public class ScheduleCell
{
    public Guid Id { get; set; }
    public int EntryType { get; set; }
    public Guid SourceId { get; set; }
    public Guid ClientId { get; set; }
    public DateTime EntryDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal? ChangeTime { get; set; }
    public int? WorkChangeType { get; set; }
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public bool? ToInvoice { get; set; }
    public bool? Taxable { get; set; }
    public Guid ShiftId { get; set; }
    public string? EntryName { get; set; }
    public string? Abbreviation { get; set; }
    public Guid? ReplaceClientId { get; set; }
    public bool IsReplacementEntry { get; set; }
}
