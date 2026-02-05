namespace Klacks.Api.Presentation.DTOs.Schedules;

public class DayApprovalResource
{
    public Guid Id { get; set; }
    public DateOnly ApprovalDate { get; set; }
    public Guid GroupId { get; set; }
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime ApprovedAt { get; set; }
}
