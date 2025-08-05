using Klacks.Api.Resources.Settings;

namespace Klacks.Api.Resources.Filter
{
    public class FilterResource : BaseFilter
    {
        public bool? ActiveMembership { get; set; }

        public int? ClientType { get; set; }

        public bool? CompanyAddress { get; set; }

        public List<CountryResource> Countries { get; set; } = new List<CountryResource>();
        
        public bool CountriesHaveBeenReadIn { get; set; }
        
        public bool? Female { get; set; }
        
        public List<StateCountryToken> FilteredStateToken { get; set; } = new List<StateCountryToken>();
        
        public bool? FormerMembership { get; set; }
        
        public bool? FutureMembership { get; set; }
        
        public bool? HasAnnotation { get; set; }
        
        public bool? HomeAddress { get; set; }
        
        public bool IncludeAddress { get; set; }
        
        public bool? InvoiceAddress { get; set; }
        
        public string Language { get; set; } = string.Empty;
        
        public bool? LegalEntity { get; set; }
        
        public List<StateCountryToken> List { get; set; } = new List<StateCountryToken>();
        
        public string MacroFilter { get; set; } = string.Empty;
        
        public bool? Male { get; set; }
        
        public DateTime? ScopeFrom { get; set; }
        
        public bool? ScopeFromFlag { get; set; }
        
        public DateTime? ScopeUntil { get; set; }
        
        public bool? ScopeUntilFlag { get; set; }
        
        public bool? SearchOnlyByName { get; set; } = null;
        
        public string SearchString { get; set; } = string.Empty;
        
        public bool ShowDeleteEntries { get; set; }
        
        public Guid? SelectedGroup { get; set; }
    }
}
