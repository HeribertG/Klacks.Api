using Klacks.Api.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Staffs;

namespace Klacks.Api.Presentation.DTOs.Schedules
{
    public class WorkResource
    {
        public ClientResource? Client { get; set; }

        public Guid ClientId { get; set; }

        public DateTime From { get; set; }

        public Guid Id { get; set; }

        public string? Information { get; set; }

        public bool IsSealed { get; set; }

        public ShiftResource? Shift { get; set; }

        public Guid ShiftId { get; set; }

        public DateTime Until { get; set; }
    }
}
