using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Presentation.DTOs.Schedules;

public class WorkResource
{
    public ClientResource? Client { get; set; }

    public Guid ClientId { get; set; }

    public DateTime CurrentDate { get; set; }

    public Guid Id { get; set; }

    public string? Information { get; set; }

    public bool IsSealed { get; set; }

    public ShiftResource? Shift { get; set; }

    public Guid ShiftId { get; set; }

    public decimal WorkTime { get; set; }

    public decimal Surcharges { get; set; }

    public TimeOnly StartShift { get; set; }

    public TimeOnly EndShift { get; set; }
}
