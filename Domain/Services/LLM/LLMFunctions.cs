using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Domain.Services.LLM;

public static class LLMFunctions
{
    public static LLMFunction CreateClient => new()
    {
        Name = "create_client",
        Description = "Erstellt einen neuen Mitarbeiter oder Kunden im System mit allen Daten (Name, Adresse, Geburtsdatum, Vertrag, Gruppe). Wichtig: Für Schweizer Adressen erkenne den Kanton automatisch aus der PLZ (z.B. 3097 Liebefeld → Kanton BE/Bern). Setze immer das Land korrekt.",
        Parameters = new Dictionary<string, object>
        {
            ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Vorname des Mitarbeiters" },
            ["lastName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Nachname des Mitarbeiters" },
            ["gender"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["enum"] = new[] { "Male", "Female", "Intersexuality", "LegalEntity" },
                ["description"] = "Geschlecht - LegalEntity für Firmen verwenden"
            },
            ["birthdate"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Geburtsdatum im Format YYYY-MM-DD (z.B. 1959-10-25)" },
            ["street"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Strasse und Hausnummer" },
            ["postalCode"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Postleitzahl (z.B. 3097)" },
            ["city"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Stadt/Ort (z.B. Liebefeld)" },
            ["canton"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["description"] = "Schweizer Kanton - erkenne aus PLZ: 1xxx=VD/GE, 2xxx=NE/JU, 3xxx=BE, 4xxx=BS/BL/SO, 5xxx=AG, 6xxx=LU/ZG/SZ/NW/OW/UR, 7xxx=GR, 8xxx=ZH/TG/SH, 9xxx=SG/AR/AI"
            },
            ["country"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Land - IMMER setzen (z.B. Schweiz, Deutschland, Österreich)" },
            ["contractType"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["description"] = "Vertragstyp falls gewünscht (z.B. 'BE 180 Std', 'ZH 160 Std')"
            },
            ["groupPath"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["description"] = "Gruppenpfad falls gewünscht (z.B. 'Deutschweiz Mitte -> BERN -> Bern')"
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

    public static LLMFunction CreateSystemUser => new()
    {
        Name = "create_system_user",
        Description = "Creates a new system user (login account) through the Settings UI. " +
                      "Opens Settings → User Administration, fills the form and saves. Returns username and password.",
        Parameters = new Dictionary<string, object>
        {
            ["firstName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "First name of the user" },
            ["lastName"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Last name of the user" },
            ["email"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Email address of the user" }
        },
        RequiredParameters = new List<string> { "firstName", "lastName", "email" }
    };

    public static LLMFunction DeleteSystemUser => new()
    {
        Name = "delete_system_user",
        Description = "Deletes a system user through the Settings UI. Opens Settings → User Administration, clicks the delete button and confirms.",
        Parameters = new Dictionary<string, object>
        {
            ["userId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ID of the user to delete" }
        },
        RequiredParameters = new List<string> { "userId" }
    };

    public static LLMFunction ListSystemUsers => new()
    {
        Name = "list_system_users",
        Description = "Lists all system users from the Settings UI. Opens Settings → User Administration and reads the user list.",
        Parameters = new Dictionary<string, object>(),
        RequiredParameters = new List<string>()
    };

    public static LLMFunction CreateBranch => new()
    {
        Name = "create_branch",
        Description = "Creates a new branch through the Settings UI. Opens Settings → Branches, opens the modal, fills it and saves.",
        Parameters = new Dictionary<string, object>
        {
            ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Name of the branch" },
            ["address"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Address of the branch" },
            ["phone"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Phone number" },
            ["email"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Email address" }
        },
        RequiredParameters = new List<string> { "name", "address" }
    };

    public static LLMFunction DeleteBranch => new()
    {
        Name = "delete_branch",
        Description = "Deletes a branch through the Settings UI. Clicks the delete button and confirms.",
        Parameters = new Dictionary<string, object>
        {
            ["branchId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ID of the branch to delete" }
        },
        RequiredParameters = new List<string> { "branchId" }
    };

    public static LLMFunction ListBranches => new()
    {
        Name = "list_branches",
        Description = "Lists all branches from the Settings UI. Opens Settings and reads the branch list.",
        Parameters = new Dictionary<string, object>(),
        RequiredParameters = new List<string>()
    };

    public static LLMFunction CreateMacro => new()
    {
        Name = "create_macro",
        Description = "Creates a new macro with KlacksScript code through the Settings UI. " +
                      "Macros are VB.NET-like scripts that calculate shift surcharges, work rules, and break rules. " +
                      "Available variables (via import): hour (work hours), fromhour/untilhour (start/end as decimal hours), " +
                      "weekday (1=Mon..7=Sun), holiday/holidaynextday (boolean), nightrate, holidayrate, sarate (Samstag/Saturday surcharge), sorate (Sonntag/Sunday surcharge), " +
                      "guaranteedhours, fulltime. " +
                      "Available functions: TimeToHours('08:30')→8.5, TimeOverlap(s1,e1,s2,e2) for time range overlap, " +
                      "IIF(cond,true,false), Abs, Round, Len, Left, Right, Mid, InStr, Replace, Trim. " +
                      "Control: IF-THEN-ELSE-ENDIF, SELECT CASE, FOR-NEXT, DO-WHILE/UNTIL-LOOP, FUNCTION-ENDFUNCTION, SUB-ENDSUB. " +
                      "Output: OUTPUT type, value (type 1=DefaultResult, 5=Info, 8000=Filter). Debug: DEBUGPRINT value. " +
                      "IMPORTANT: DIM cannot initialize with a value (like old VB/VBA). Use: DIM varname on one line, then varname = value on the next. " +
                      "IMPORTANT: Ask the user what the macro should calculate before writing code. " +
                      "Example: dim rate / rate = 0 / if weekday >= 6 then rate = sorate / endif / OUTPUT 1, rate",
        Parameters = new Dictionary<string, object>
        {
            ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "Name of the macro" },
            ["type"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["enum"] = new[] { "ShiftAndEmployments", "WorkRules" },
                ["description"] = "Type: ShiftAndEmployments (shift surcharges) or WorkRules (work rule checks). Default: ShiftAndEmployments"
            },
            ["content"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "KlacksScript code for the macro. Use \\n for newlines." }
        },
        RequiredParameters = new List<string> { "name" }
    };

    public static LLMFunction DeleteMacro => new()
    {
        Name = "delete_macro",
        Description = "Deletes a macro through the Settings UI. Clicks the delete button and confirms.",
        Parameters = new Dictionary<string, object>
        {
            ["macroId"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "ID of the macro to delete" }
        },
        RequiredParameters = new List<string> { "macroId" }
    };

    public static LLMFunction ListMacros => new()
    {
        Name = "list_macros",
        Description = "Lists all macros from the Settings UI. Opens Settings and reads the macro list.",
        Parameters = new Dictionary<string, object>(),
        RequiredParameters = new List<string>()
    };

    public static LLMFunction SearchAndNavigate => new()
    {
        Name = "searchAndNavigate",
        Description = "Sucht nach einer Entität (Mitarbeiter, Kunde, Gruppe, Dienst) anhand des Namens und navigiert direkt dorthin. Verwende diese Funktion wenn der Benutzer eine Person oder Entität öffnen/bearbeiten möchte (z.B. 'Öffne den Kunden Max Müller', 'Zeige mir Heribert Gasparoli'). Bei mehreren Treffern werden alle angezeigt.",
        Parameters = new Dictionary<string, object>
        {
            ["entityType"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["enum"] = new[] { "client", "shift", "group" },
                ["description"] = "Typ der zu suchenden Entität: client (Mitarbeiter/Kunden), shift (Dienste), group (Gruppen)"
            },
            ["searchQuery"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["description"] = "Name oder Suchbegriff um die Entität zu finden"
            },
            ["action"] = new Dictionary<string, object> {
                ["type"] = "string",
                ["enum"] = new[] { "view", "edit" },
                ["description"] = "Aktion nach dem Finden: view (anzeigen) oder edit (bearbeiten). Standard ist edit."
            }
        },
        RequiredParameters = new List<string> { "entityType", "searchQuery" }
    };
}