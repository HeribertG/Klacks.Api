using Klacks_api.Resources.Filter;
using Klacks_api.Resources.Staffs;

namespace Klacks.Api.Resources.Filter
{
    public class TruncatedClientResource : BaseTruncatedResult
    {
        public ICollection<ClientResource>? Clients { get; set; }

        public string Editor { get; set; } = string.Empty;
        public DateTime LastChange { get; set; }
    }
}
