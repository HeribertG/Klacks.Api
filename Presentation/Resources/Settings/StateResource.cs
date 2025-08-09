using Klacks.Api.Datas;

namespace Klacks.Api.Presentation.Resources.Settings
{
    public class StateResource
    {
        public string Abbreviation { get; set; } = string.Empty;
        public string CountryPrefix { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public MultiLanguage Name { get; set; } = null!;
    }
}
