using System.Text.Json;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Presentation.DTOs.LLM;
using Klacks.Api.Presentation.DTOs.Staffs;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Application.Commands;
using MediatR;

namespace Klacks.Api.Domain.Services.LLM;

public class LLMService : ILLMService
{
    private readonly IMediator _mediator;
    private readonly ILogger<LLMService> _logger;
    private readonly IConfiguration _configuration;

    public LLMService(IMediator mediator, ILogger<LLMService> logger, IConfiguration configuration)
    {
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<LLMResponse> ProcessAsync(LLMContext context)
    {
        try
        {
            _logger.LogInformation("Processing LLM request from user {UserId}: {Message}", 
                context.UserId, context.Message);

            // Einfache Keyword-basierte Verarbeitung (später durch echtes LLM ersetzen)
            var message = context.Message.ToLowerInvariant();

            if (message.Contains("erstelle") && message.Contains("mitarbeiter"))
            {
                return await HandleCreateClient(context);
            }
            
            if (message.Contains("suche") || message.Contains("finde"))
            {
                return await HandleSearchClients(context);
            }
            
            if (message.Contains("hilfe") || message.Contains("help"))
            {
                return HandleHelp(context);
            }

            if (message.Contains("navigiere") || message.Contains("öffne") || message.Contains("gehe zu"))
            {
                return HandleNavigation(context);
            }

            // Standard-Antwort für unverstandene Anfragen
            return GetDefaultResponse();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing LLM request for user {UserId}", context.UserId);
            return new LLMResponse 
            { 
                Message = "❌ Entschuldigung, bei der Verarbeitung Ihrer Anfrage ist ein Fehler aufgetreten."
            };
        }
    }

    private LLMResponse GetDefaultResponse()
    {
        return new LLMResponse 
        { 
            Message = "Entschuldigung, ich habe Ihre Anfrage nicht verstanden. Sie können mich bitten:\n\n" +
                     "• **Mitarbeiter erstellen**: 'Erstelle Mitarbeiter Max Muster'\n" +
                     "• **Personen suchen**: 'Suche nach Hans'\n" +
                     "• **Navigation**: 'Öffne Mitarbeiter-Seite'\n" +
                     "• **Hilfe**: 'Hilfe' für detaillierte Informationen",
            Suggestions = new List<string>
            {
                "Erstelle einen Mitarbeiter",
                "Suche nach Personen aus Zürich", 
                "Öffne Dashboard",
                "Zeige mir die Hilfe"
            }
        };
    }

    private async Task<LLMResponse> HandleCreateClient(LLMContext context)
    {
        // Extrahiere Parameter aus der Nachricht (vereinfacht)
        var message = context.Message;
        
        // Einfache Pattern-Erkennung
        var firstName = ExtractFirstName(message);
        var lastName = ExtractLastName(message);
        
        if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName))
        {
            return new LLMResponse 
            { 
                Message = "Um einen Mitarbeiter zu erstellen, benötige ich mindestens Vor- und Nachname.\n\n" +
                         "**Beispiel:** 'Erstelle Mitarbeiter Hans Muster'",
                Suggestions = new List<string>
                {
                    "Erstelle Mitarbeiter Max Muster",
                    "Erstelle Mitarbeiter Anna Weber",
                    "Zeige mir alle Mitarbeiter"
                }
            };
        }

        try
        {
            // Mock-Implementation - später durch echte Erstellung ersetzen
            _logger.LogInformation("Creating client: {FirstName} {LastName}", firstName, lastName);
            
            return new LLMResponse 
            { 
                Message = $"✅ **Mitarbeiter erfolgreich erstellt**\n\n" +
                         $"**Name:** {firstName} {lastName}\n" +
                         $"**Status:** Aktiv\n" +
                         $"**Typ:** Employee\n\n" +
                         $"Der Mitarbeiter wurde im System angelegt und kann nun verwendet werden.",
                NavigateTo = "/clients",
                ActionPerformed = true,
                Suggestions = new List<string>
                {
                    "Zeige alle Mitarbeiter",
                    "Erstelle weiteren Mitarbeiter",
                    "Suche nach " + firstName
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating client {FirstName} {LastName}", firstName, lastName);
            return new LLMResponse 
            { 
                Message = $"❌ **Fehler beim Erstellen**\n\nBeim Erstellen des Mitarbeiters {firstName} {lastName} ist ein Fehler aufgetreten. Bitte versuchen Sie es erneut."
            };
        }
    }

    private async Task<LLMResponse> HandleSearchClients(LLMContext context)
    {
        var message = context.Message;
        var searchTerm = ExtractSearchTerm(message);
        
        if (string.IsNullOrEmpty(searchTerm))
        {
            return new LLMResponse 
            { 
                Message = "Nach was möchten Sie suchen?\n\n**Beispiele:**\n• 'Suche nach Hans'\n• 'Finde alle Müller'\n• 'Zeige Personen aus Zürich'",
                Suggestions = new List<string>
                {
                    "Suche nach Hans",
                    "Finde alle aus Zürich", 
                    "Zeige alle Mitarbeiter"
                }
            };
        }

        try
        {
            _logger.LogInformation("Searching for clients with term: {SearchTerm}", searchTerm);

            // Mock-Implementation - später durch echte Suche ersetzen
            var mockResults = new List<string>
            {
                "Hans Muster (Mitarbeiter)",
                "Anna Müller (Kunde)",
                "Max Weber (Externer)"
            }.Where(r => r.ToLowerInvariant().Contains(searchTerm.ToLowerInvariant())).ToList();

            var responseMessage = mockResults.Any() 
                ? $"🔍 **Suchergebnisse für '{searchTerm}'**\n\n{string.Join("\n• ", mockResults.Select(r => $"• {r}"))}"
                : $"❌ **Keine Ergebnisse gefunden**\n\nFür '{searchTerm}' wurden keine Personen gefunden.";

            return new LLMResponse 
            { 
                Message = responseMessage,
                NavigateTo = mockResults.Any() ? "/clients" : null,
                Suggestions = mockResults.Any() 
                    ? new List<string> { "Zeige Details", "Neue Suche", "Alle Mitarbeiter anzeigen" }
                    : new List<string> { "Alle Mitarbeiter anzeigen", "Neue Suche versuchen", "Neuen Mitarbeiter erstellen" }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for clients with term: {SearchTerm}", searchTerm);
            return new LLMResponse 
            { 
                Message = "❌ Bei der Suche ist ein Fehler aufgetreten. Bitte versuchen Sie es erneut."
            };
        }
    }

    private LLMResponse HandleNavigation(LLMContext context)
    {
        var message = context.Message.ToLowerInvariant();
        
        var navigationMap = new Dictionary<string, (string path, string description)>
        {
            ["dashboard"] = ("/dashboard", "Dashboard"),
            ["mitarbeiter"] = ("/clients", "Mitarbeiter-Übersicht"),
            ["kunde"] = ("/clients", "Kunden-Übersicht"),
            ["vertrag"] = ("/contracts", "Vertrags-Verwaltung"),
            ["einstellung"] = ("/settings", "Einstellungen"),
            ["kalender"] = ("/calendar", "Kalender")
        };

        foreach (var nav in navigationMap)
        {
            if (message.Contains(nav.Key))
            {
                return new LLMResponse 
                { 
                    Message = $"📍 **Navigation zu {nav.Value.description}**\n\nIch öffne die {nav.Value.description} für Sie.",
                    NavigateTo = nav.Value.path,
                    ActionPerformed = true
                };
            }
        }

        return new LLMResponse 
        { 
            Message = "Wohin möchten Sie navigieren?\n\n**Verfügbare Bereiche:**\n• Dashboard\n• Mitarbeiter\n• Kunden\n• Verträge\n• Einstellungen\n• Kalender",
            Suggestions = new List<string>
            {
                "Öffne Dashboard",
                "Zeige Mitarbeiter",
                "Gehe zu Einstellungen"
            }
        };
    }

    private LLMResponse HandleHelp(LLMContext context)
    {
        var userRights = string.Join(", ", context.UserRights);
        
        return new LLMResponse 
        { 
            Message = "🤖 **Klacks KI-Assistent - Hilfe**\n\n" +
                     $"**Ihre Berechtigungen:** {userRights}\n\n" +
                     "**💼 Mitarbeiter-Verwaltung:**\n" +
                     "• 'Erstelle Mitarbeiter Max Muster'\n" +
                     "• 'Suche nach Hans'\n" +
                     "• 'Zeige alle aus Zürich'\n\n" +
                     "**🧭 Navigation:**\n" +
                     "• 'Öffne Dashboard'\n" +
                     "• 'Zeige Mitarbeiter'\n" +
                     "• 'Gehe zu Einstellungen'\n\n" +
                     "**ℹ️ Allgemein:**\n" +
                     "• 'Hilfe' - Diese Hilfe anzeigen\n" +
                     "• Sie können natürlich mit mir sprechen!",
            Suggestions = new List<string>
            {
                "Erstelle Mitarbeiter Max Muster",
                "Suche nach Personen aus Bern", 
                "Öffne Dashboard",
                "Zeige alle Mitarbeiter"
            }
        };
    }

    #region Helper Methods für Pattern Recognition

    private string ExtractFirstName(string message)
    {
        // Vereinfachte Extraktion - später durch NLP ersetzen
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var keywordIndex = Array.FindIndex(words, w => 
            w.ToLowerInvariant().Contains("mitarbeiter") || 
            w.ToLowerInvariant().Contains("erstelle"));
        
        if (keywordIndex >= 0 && keywordIndex + 1 < words.Length)
        {
            return words[keywordIndex + 1];
        }
        
        return string.Empty;
    }

    private string ExtractLastName(string message)
    {
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var keywordIndex = Array.FindIndex(words, w => 
            w.ToLowerInvariant().Contains("mitarbeiter") || 
            w.ToLowerInvariant().Contains("erstelle"));
        
        if (keywordIndex >= 0 && keywordIndex + 2 < words.Length)
        {
            return words[keywordIndex + 2];
        }
        
        return string.Empty;
    }

    private string ExtractSearchTerm(string message)
    {
        // Vereinfachte Extraktion
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var keywordIndex = Array.FindIndex(words, w => 
            w.ToLowerInvariant().Contains("suche") || 
            w.ToLowerInvariant().Contains("finde") ||
            w.ToLowerInvariant().Contains("nach"));
        
        if (keywordIndex >= 0 && keywordIndex + 1 < words.Length)
        {
            // Nimm das nächste Wort nach "suche nach" oder "finde"
            var nextWordIndex = keywordIndex + 1;
            if (words[keywordIndex].ToLowerInvariant() == "nach" && keywordIndex > 0)
            {
                nextWordIndex = keywordIndex + 1;
            }
            else if (words[nextWordIndex].ToLowerInvariant() == "nach" && keywordIndex + 2 < words.Length)
            {
                nextWordIndex = keywordIndex + 2;
            }
            
            return nextWordIndex < words.Length ? words[nextWordIndex] : string.Empty;
        }
        
        return string.Empty;
    }

    private string ExtractCanton(string message)
    {
        var cantons = new[] { "BE", "ZH", "SG", "VD", "AG", "LU", "BS", "Bern", "Zürich", "St. Gallen", "Waadt", "Aargau", "Luzern", "Basel" };
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        return cantons.FirstOrDefault(canton => 
            words.Any(word => word.Equals(canton, StringComparison.OrdinalIgnoreCase))) ?? string.Empty;
    }

    #endregion
}