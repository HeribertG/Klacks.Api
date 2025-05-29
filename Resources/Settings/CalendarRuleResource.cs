namespace Klacks.Api.Resources.Settings
{
    public class CalendarRuleResource
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Rule { get; set; } = string.Empty;
        public string SubRule { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
        public bool IsPaid { get; set; }
        public int State { get; set; } = 0;
    }
}
