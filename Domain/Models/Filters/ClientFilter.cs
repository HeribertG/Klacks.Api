// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Filters;

public class ClientFilter
{
    public string SearchString { get; set; } = string.Empty;

    public bool? SearchOnlyByName { get; set; }

    public bool? ActiveMembership { get; set; }

    public bool? FormerMembership { get; set; }

    public bool? FutureMembership { get; set; }

    public DateTime? ScopeFrom { get; set; }

    public DateTime? ScopeUntil { get; set; }

    public int? ClientType { get; set; }

    public bool? LegalEntity { get; set; }

    public bool? Male { get; set; }

    public bool? Female { get; set; }

    public bool? Intersexuality { get; set; }

    public bool Employee { get; set; } = true;

    public bool ExternEmp { get; set; } = true;

    public bool Customer { get; set; } = true;

    public bool ShowDeleteEntries { get; set; }

    public Guid? SelectedGroup { get; set; }

    public List<string> FilteredCantons { get; set; } = new List<string>();
    
    public List<string> Countries { get; set; } = new List<string>();
    
    public List<StateCountryFilter> FilteredStateToken { get; set; } = new List<StateCountryFilter>();
    
    public bool CountriesHaveBeenReadIn { get; set; }

    public string Language { get; set; } = string.Empty;

    public string MacroFilter { get; set; } = string.Empty;

    public bool? HasAnnotation { get; set; }

    public bool? CompanyAddress { get; set; }
    public bool? HomeAddress { get; set; }
    public bool? InvoiceAddress { get; set; }
    public bool IncludeAddress { get; set; }
    public bool? ScopeFromFlag { get; set; }
    public bool? ScopeUntilFlag { get; set; }
    public string OrderBy { get; set; } = string.Empty;
    public string SortOrder { get; set; } = string.Empty;
}