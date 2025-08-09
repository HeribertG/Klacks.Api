using Klacks.Api.Models.Staffs;

namespace Klacks.Api.Presentation.Resources.Filter
{
    public class TruncatedClient : BaseTruncatedResult
    {
        public ICollection<Client>? Clients { get; set; }

        public string Editor { get; set; } = string.Empty;

        public DateTime LastChange { get; set; }
    }
}
