namespace Klacks.Api.Models.Settings
{
    public class MacroType
    {

        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public int Type { get; set; }

    }
}
