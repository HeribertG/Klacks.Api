using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM;

public static class LLMFunctions
{
    public static LLMFunction CreateClient => new()
    {
        Name = "create_client",
        Description = "Erstellt einen neuen Mitarbeiter oder Kunden im System",
        Parameters = new Dictionary<string, object>
        {
            ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Vorname des Mitarbeiters" },
            ["lastName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Nachname des Mitarbeiters" },
            ["gender"] = new Dictionary<string, object> { 
                ["type"] = "string", 
                ["enum"] = new[] { "Male", "Female", "Intersexuality", "LegalEntity" },
                ["description"] = "Geschlecht - LegalEntity für Firmen verwenden"
            }
        },
        RequiredParameters = new List<string> { "firstName", "lastName", "gender" }
    };

    public static LLMFunction SearchClients => new()
    {
        Name = "search_clients",
        Description = "Sucht nach Mitarbeitern oder Kunden anhand verschiedener Kriterien",
        Parameters = new Dictionary<string, object>
        {
            ["searchTerm"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Suchbegriff für Name oder Firma" },
            ["canton"] = new Dictionary<string, object> { 
                ["type"] = "string", 
                ["enum"] = new[] { "BE", "ZH", "SG", "VD", "AG", "LU", "BS" },
                ["description"] = "Schweizer Kanton filtern"
            },
            ["membershipType"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["enum"] = new[] { "Employee", "Customer", "ExternEmp" },
                ["description"] = "Art der Mitgliedschaft"
            }
        },
        RequiredParameters = new List<string>()
    };

    public static LLMFunction CreateContract => new()
    {
        Name = "create_contract",
        Description = "Erstellt einen neuen Arbeitsvertrag für einen Mitarbeiter",
        Parameters = new Dictionary<string, object>
        {
            ["clientId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ID des Mitarbeiters" },
            ["contractType"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["enum"] = new[] { "Vollzeit 160", "Vollzeit 180", "Teilzeit 0 Std" },
                ["description"] = "Art des Vertrags"
            },
            ["canton"] = new Dictionary<string, object> { 
                ["type"] = "string", 
                ["enum"] = new[] { "BE", "ZH", "SG", "VD", "AG", "LU", "BS" },
                ["description"] = "Kanton für den Vertrag"
            }
        },
        RequiredParameters = new List<string> { "clientId", "contractType", "canton" }
    };

    public static LLMFunction GetSystemInfo => new()
    {
        Name = "get_system_info",
        Description = "Liefert allgemeine Informationen über das Klacks HR-System",
        Parameters = new Dictionary<string, object>(),
        RequiredParameters = new List<string>()
    };

    public static LLMFunction NavigateToPage => new()
    {
        Name = "navigate_to_page",
        Description = "Navigiert zu verschiedenen Seiten in der Anwendung",
        Parameters = new Dictionary<string, object>
        {
            ["page"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["enum"] = new[] { "dashboard", "clients", "contracts", "settings", "calendar", "reports" },
                ["description"] = "Zielseite für die Navigation"
            }
        },
        RequiredParameters = new List<string> { "page" }
    };
}