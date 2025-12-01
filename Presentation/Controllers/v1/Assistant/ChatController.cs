using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MediatR;
using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Presentation.DTOs.LLM;
using Klacks.Api.Domain.Services.LLM;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.v1.Assistant;

[ApiController]
[Route("api/v1/backend/assistant/chat")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)] 
public class ChatController : ControllerBase
{
    private readonly ILogger<ChatController> _logger;
    private readonly IMediator _mediator;

    public ChatController(ILogger<ChatController> logger, IMediator mediator)
    {
        this._logger = logger;
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<LLMResponse>> ProcessMessage([FromBody] LLMRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("Message cannot be empty");
        }

        // User-Kontext aus dem JWT Token extrahieren
        var userId = GetCurrentUserId();
        var userRights = GetCurrentUserRights();
        
        _logger.LogInformation("Processing assistant request for user {UserId}: {Message}", userId, request.Message);
        
        var response = await _mediator.Send(new ProcessLLMMessageCommand
        {
            Message = request.Message,
            UserId = userId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            Language = request.Language,
            UserRights = userRights
        });
        
        response.ConversationId = request.ConversationId ?? Guid.NewGuid().ToString();
        
        return Ok(response);
    }

    [HttpGet("functions")]
    public ActionResult<List<LLMFunction>> GetAvailableFunctions()
    {
        var userRights = GetCurrentUserRights();
        var functions = GetAvailableFunctions(userRights);
        return Ok(functions);
    }

    [HttpPost("execute-function")]
    public async Task<ActionResult<LLMFunctionResult>> ExecuteFunction([FromBody] LLMFunctionExecuteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FunctionName))
        {
            return BadRequest("Function name cannot be empty");
        }

        var userId = GetCurrentUserId();
        _logger.LogInformation("Executing function {FunctionName} for user {UserId}", request.FunctionName, userId);

        var response = await _mediator.Send(new ExecuteLLMFunctionCommand
        {
            FunctionName = request.FunctionName,
            Parameters = request.Parameters,
            UserId = userId
        });

        return Ok(response);
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

    private string GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null)
        {
            return userIdClaim.Value;
        }
        
        return string.Empty;
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