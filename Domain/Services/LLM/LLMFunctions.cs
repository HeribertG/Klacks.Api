using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM;

public static class LLMFunctions
{
    public static LLMFunction CreateClient => new()
    {
        Name = "create_client",
        Description = "Erstellt einen neuen Mitarbeiter oder Kunden im System",
        Parameters = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["firstName"] = new { type = "string", description = "Vorname des Mitarbeiters" },
                ["lastName"] = new { type = "string", description = "Nachname des Mitarbeiters" },
                ["gender"] = new { 
                    type = "string", 
                    @enum = new[] { "Male", "Female", "Intersexuality", "LegalEntity" },
                    description = "Geschlecht - LegalEntity für Firmen verwenden"
                },
                ["company"] = new { type = "string", description = "Firmenname (nur bei LegalEntity erforderlich)" },
                ["canton"] = new { 
                    type = "string", 
                    @enum = new[] { "BE", "ZH", "SG", "VD", "AG", "LU", "BS" },
                    description = "Schweizer Kanton für die Adresse"
                },
                ["street"] = new { type = "string", description = "Strassenname" },
                ["streetNumber"] = new { type = "string", description = "Hausnummer" },
                ["city"] = new { type = "string", description = "Wohnort" },
                ["zip"] = new { type = "string", description = "Postleitzahl" },
                ["email"] = new { type = "string", description = "E-Mail Adresse" },
                ["phone"] = new { type = "string", description = "Telefonnummer" },
                ["membershipType"] = new {
                    type = "string",
                    @enum = new[] { "Employee", "Customer", "ExternEmp" },
                    description = "Art der Mitgliedschaft"
                }
            },
            required = new[] { "firstName", "lastName", "gender" }
        }
    };

    public static LLMFunction SearchClients => new()
    {
        Name = "search_clients", 
        Description = "Sucht nach Mitarbeitern und Kunden im System",
        Parameters = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["searchTerm"] = new { type = "string", description = "Suchbegriff für Name oder Firma" },
                ["gender"] = new { 
                    type = "string", 
                    @enum = new[] { "Male", "Female", "Intersexuality", "LegalEntity" } 
                },
                ["canton"] = new { 
                    type = "string", 
                    @enum = new[] { "BE", "ZH", "SG", "VD", "AG", "LU", "BS" } 
                },
                ["membershipType"] = new {
                    type = "string",
                    @enum = new[] { "Employee", "Customer", "ExternEmp" }
                },
                ["limit"] = new { type = "integer", description = "Anzahl Ergebnisse (max 50)" }
            }
        }
    };

    public static LLMFunction CreateContract => new()
    {
        Name = "create_contract",
        Description = "Erstellt einen neuen Arbeitsvertrag",
        Parameters = new
        {
            type = "object", 
            properties = new Dictionary<string, object>
            {
                ["name"] = new { type = "string", description = "Vertragsname" },
                ["canton"] = new { 
                    type = "string", 
                    @enum = new[] { "BE", "ZH", "SG", "VD" },
                    description = "Kanton für Kalender-Auswahl"
                },
                ["contractType"] = new {
                    type = "string",
                    @enum = new[] { "Vollzeit 160", "Vollzeit 180", "Teilzeit 0 Std" },
                    description = "Vertragstyp"
                },
                ["guaranteedHours"] = new { type = "number", description = "Garantierte Stunden pro Monat" },
                ["maxHours"] = new { type = "number", description = "Maximale Stunden pro Monat" },
                ["minHours"] = new { type = "number", description = "Minimale Stunden pro Monat" }
            },
            required = new[] { "name", "canton", "contractType" }
        }
    };

    public static LLMFunction GetSystemInfo => new()
    {
        Name = "get_system_info",
        Description = "Liefert Informationen über das System und verfügbare Optionen",
        Parameters = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["infoType"] = new {
                    type = "string",
                    @enum = new[] { "cantons", "contracts", "genders", "membershipTypes", "help" },
                    description = "Art der gewünschten Information"
                }
            }
        }
    };

    public static LLMFunction NavigateToPage => new()
    {
        Name = "navigate_to_page",
        Description = "Navigiert zu einer bestimmten Seite in der Anwendung",
        Parameters = new
        {
            type = "object",
            properties = new Dictionary<string, object>
            {
                ["page"] = new {
                    type = "string",
                    @enum = new[] { "dashboard", "employees", "customers", "contracts", "settings", "calendar" },
                    description = "Zielseite für die Navigation"
                }
            },
            required = new[] { "page" }
        }
    };

    public static List<LLMFunction> GetAllFunctions()
    {
        return new List<LLMFunction>
        {
            CreateClient,
            SearchClients,
            CreateContract,
            GetSystemInfo,
            NavigateToPage
        };
    }
}