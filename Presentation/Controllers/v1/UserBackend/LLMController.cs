using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Presentation.DTOs.LLM;
using Klacks.Api.Domain.Services.LLM;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

[ApiController]
[Route("api/v1/llm")]
[Authorize] // User muss eingeloggt sein
public class LLMController : ControllerBase
{
    private readonly ILLMService _llmService;
    private readonly ILogger<LLMController> _logger;

    public LLMController(ILLMService llmService, ILogger<LLMController> logger)
    {
        _llmService = llmService;
        _logger = logger;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<LLMResponse>> ProcessMessage([FromBody] LLMRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            // User-Kontext aus dem JWT Token extrahieren
            var userId = GetCurrentUserId();
            var userRights = GetCurrentUserRights();
            
            var llmContext = new LLMContext
            {
                Message = request.Message,
                UserId = userId,
                UserRights = userRights,
                AvailableFunctions = GetAvailableFunctions(userRights),
                ConversationId = request.ConversationId
            };
            
            _logger.LogInformation("Processing LLM request for user {UserId}: {Message}", userId, request.Message);
            
            var response = await _llmService.ProcessAsync(llmContext);
            
            response.ConversationId = request.ConversationId ?? Guid.NewGuid().ToString();
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing LLM request: {Message}", request.Message);
            return StatusCode(500, new LLMResponse 
            { 
                Message = "Entschuldigung, es ist ein interner Fehler aufgetreten." 
            });
        }
    }

    [HttpGet("functions")]
    public ActionResult<List<LLMFunction>> GetAvailableFunctions()
    {
        var userRights = GetCurrentUserRights();
        var functions = GetAvailableFunctions(userRights);
        return Ok(functions);
    }

    [HttpGet("help")]
    public ActionResult<object> GetHelp()
    {
        return Ok(new 
        {
            SupportedCommands = new[]
            {
                "Erstelle Mitarbeiter [Vorname] [Nachname] aus [Kanton]",
                "Suche nach [Begriff]", 
                "Zeige Personen aus [Kanton]",
                "Erstelle Vertrag [Typ] für [Kanton]"
            },
            SupportedCantons = new[] { "BE", "ZH", "SG", "VD" },
            ContractTypes = new[] { "Vollzeit 160", "Vollzeit 180", "Teilzeit 0 Std" },
            Examples = new[]
            {
                "Erstelle Mitarbeiter Hans Muster aus Zürich",
                "Suche nach Müller",
                "Zeige alle Personen aus Bern",
                "Erstelle Vollzeit 160 Vertrag für Zürich"
            }
        });
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        
        // Fallback - sollte nicht vorkommen bei korrekt authentifizierten Requests
        return Guid.Empty;
    }

    private List<string> GetCurrentUserRights()
    {
        // Extrahiere User-Rechte aus Claims oder einem Service
        var rights = new List<string>();
        
        // Beispiel: Claims aus Token lesen
        var roleClaims = User.FindAll(ClaimTypes.Role);
        foreach (var claim in roleClaims)
        {
            rights.Add(claim.Value);
        }
        
        // Fallback für Demo-Zwecke
        if (!rights.Any())
        {
            rights.AddRange(new[] { "CanCreateClients", "CanViewClients", "CanCreateContracts" });
        }
        
        return rights;
    }

    private List<LLMFunction> GetAvailableFunctions(List<string> userRights)
    {
        var functions = new List<LLMFunction>();
        
        // Basis-Funktionen die jeder hat
        functions.Add(LLMFunctions.GetSystemInfo);
        
        // Rechte-basierte Funktionen
        if (userRights.Contains("CanViewClients") || userRights.Contains("Admin"))
        {
            functions.Add(LLMFunctions.SearchClients);
        }
        
        if (userRights.Contains("CanCreateClients") || userRights.Contains("Admin"))
        {
            functions.Add(LLMFunctions.CreateClient);
        }
        
        if (userRights.Contains("CanCreateContracts") || userRights.Contains("Admin"))
        {
            functions.Add(LLMFunctions.CreateContract);
        }
        
        return functions;
    }
}