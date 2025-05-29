using Klacks.Api.Datas;

namespace Klacks.Api.Resources.Schedules
{
    public class AbsenceResource
    {
        public string Color { get; set; } = string.Empty;
        public int DefaultLength { get; set; } = 0;
        public double DefaultValue { get; set; }
        public MultiLanguage Description { get; set; } = null!;
        public bool HideInGantt { get; set; }
        public Guid Id { get; set; }
        public MultiLanguage Name { get; set; } = null!;
        public bool Undeletable { get; set; }
        public bool WithHoliday { get; set; }
        public bool WithSaturday { get; set; }
        public bool WithSunday { get; set; }
    }
}
