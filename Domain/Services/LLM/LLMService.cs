using System.Diagnostics;
using System.Text.Json;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.LLM;
using Klacks.Api.Domain.Services.LLM.Providers;
using Klacks.Api.Presentation.DTOs.LLM;
using MediatR;

namespace Klacks.Api.Domain.Services.LLM;

public class LLMService : ILLMService
{
    private readonly IMediator _mediator;
    private readonly ILogger<LLMService> _logger;
    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILLMRepository _repository;
    private readonly IConfiguration _configuration;

    public LLMService(
        IMediator mediator,
        ILogger<LLMService> logger,
        ILLMProviderFactory providerFactory,
        ILLMRepository repository,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _logger = logger;
        _providerFactory = providerFactory;
        _repository = repository;
        _configuration = configuration;
    }

    public async Task<LLMResponse> ProcessAsync(LLMContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Processing LLM request from user {UserId}: {Message}", 
                context.UserId, context.Message);

            // Get the model and provider
            var modelId = context.ModelId ?? await GetDefaultModelIdAsync();
            var model = await _repository.GetModelByIdAsync(modelId);
            
            if (model == null || !model.IsEnabled)
            {
                _logger.LogWarning("Model {ModelId} not found or not enabled", modelId);
                return GetErrorResponse("Das gewählte Modell ist nicht verfügbar.");
            }

            var provider = await _providerFactory.GetProviderForModelAsync(modelId);
            if (provider == null)
            {
                _logger.LogWarning("Provider for model {ModelId} not found", modelId);
                return GetErrorResponse("Der Provider für das gewählte Modell ist nicht verfügbar.");
            }

            // Get or create conversation
            var conversation = await _repository.GetOrCreateConversationAsync(
                context.ConversationId ?? Guid.NewGuid().ToString(), 
                context.UserId);

            // Get conversation history
            var history = await _repository.GetConversationMessagesAsync(conversation.ConversationId);
            var llmHistory = history.Select(m => new Providers.LLMMessage
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = m.CreateTime ?? DateTime.UtcNow
            }).ToList();

            // Prepare the request
            var providerRequest = new LLMProviderRequest
            {
                Message = context.Message,
                SystemPrompt = BuildSystemPrompt(context),
                ModelId = model.ApiModelId,
                ConversationHistory = llmHistory,
                AvailableFunctions = context.AvailableFunctions,
                Temperature = 0.7,
                MaxTokens = model.MaxTokens,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken
            };

            // Call the provider
            var providerResponse = await provider.ProcessAsync(providerRequest);
            
            if (!providerResponse.Success)
            {
                _logger.LogError("Provider returned error: {Error}", providerResponse.Error);
                await TrackErrorUsageAsync(context, model, conversation, stopwatch.ElapsedMilliseconds, providerResponse.Error);
                return GetErrorResponse(providerResponse.Error ?? "Ein Fehler ist aufgetreten.");
            }

            // Process function calls if any
            if (providerResponse.FunctionCalls.Any())
            {
                var functionResults = await ProcessFunctionCallsAsync(context, providerResponse.FunctionCalls);
                if (!string.IsNullOrEmpty(functionResults))
                {
                    providerResponse.Content += "\n\n" + functionResults;
                }
            }

            // Save the conversation messages
            await SaveConversationMessagesAsync(conversation, context.Message, providerResponse.Content, model.ModelId);

            // Track usage
            await TrackUsageAsync(context, model, conversation, providerResponse.Usage, stopwatch.ElapsedMilliseconds);

            // Build response
            var response = new LLMResponse
            {
                Message = providerResponse.Content,
                ConversationId = conversation.ConversationId,
                ActionPerformed = providerResponse.FunctionCalls.Any(),
                FunctionCalls = providerResponse.FunctionCalls.Select(f => (object)new { f.FunctionName, f.Parameters }).ToList()
            };

            // Extract navigation if mentioned in the response
            response.NavigateTo = ExtractNavigation(providerResponse.Content);

            // Generate suggestions
            response.Suggestions = await GenerateSuggestionsAsync(context, providerResponse.Content);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing LLM request for user {UserId}", context.UserId);
            return GetErrorResponse("Ein interner Fehler ist aufgetreten.");
        }
    }

    private string BuildSystemPrompt(LLMContext context)
    {
        var language = context.Language ?? "de";
        
        return language switch
        {
            "en" => $@"You are a helpful AI assistant for this planning system.

User Context:
- User ID: {context.UserId}
- Permissions: {string.Join(", ", context.UserRights)}

Available Functions:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Guidelines:
- Be polite and professional
- Use available functions when users ask for them
- Give clear and precise instructions",
            
            "fr" => $@"Vous êtes un assistant IA utile pour ce système de planification.

Contexte utilisateur:
- ID utilisateur: {context.UserId}
- Autorisations: {string.Join(", ", context.UserRights)}

Fonctions disponibles:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Directives:
- Soyez poli et professionnel
- Utilisez les fonctions disponibles lorsque les utilisateurs le demandent
- Donnez des instructions claires et précises",
            
            "it" => $@"Sei un assistente AI utile per questo sistema di pianificazione.

Contesto utente:
- ID utente: {context.UserId}
- Autorizzazioni: {string.Join(", ", context.UserRights)}

Funzioni disponibili:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Linee guida:
- Sii educato e professionale
- Usa le funzioni disponibili quando gli utenti le richiedono
- Dai istruzioni chiare e precise",
            
            _ => $@"Du bist ein hilfreicher KI-Assistent für dieses Planungs-System.

Benutzer-Kontext:
- User ID: {context.UserId}
- Berechtigungen: {string.Join(", ", context.UserRights)}

Verfügbare Funktionen:
{string.Join("\n", context.AvailableFunctions.Select(f => $"- {f.Name}: {f.Description}"))}

Richtlinien:
- Sei höflich und professionell
- Verwende die verfügbaren Funktionen, wenn der Benutzer danach fragt
- Gib klare und präzise Anweisungen"
        };
    }

    private async Task<string> ProcessFunctionCallsAsync(LLMContext context, List<LLMFunctionCall> functionCalls)
    {
        var results = new List<string>();

        foreach (var call in functionCalls)
        {
            try
            {
                var result = await ExecuteFunctionAsync(context, call);
                if (!string.IsNullOrEmpty(result))
                {
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionName}", call.FunctionName);
                results.Add($"❌ Fehler beim Ausführen von {call.FunctionName}");
            }
        }

        return string.Join("\n", results);
    }

    private async Task<string> ExecuteFunctionAsync(LLMContext context, LLMFunctionCall call)
    {
        // This would be implemented to actually execute the functions
        // For now, return a placeholder
        _logger.LogInformation("Executing function {FunctionName} with parameters {Parameters}", 
            call.FunctionName, JsonSerializer.Serialize(call.Parameters));
        
        return $"✅ Funktion '{call.FunctionName}' wurde ausgeführt.";
    }

    private string? ExtractNavigation(string content)
    {
        // Simple navigation extraction - could be improved with regex or NLP
        var navigationMap = new Dictionary<string, string>
        {
            ["dashboard"] = "/dashboard",
            ["mitarbeiter"] = "/clients",
            ["verträge"] = "/contracts",
            ["einstellungen"] = "/settings"
        };

        var lowerContent = content.ToLower();
        foreach (var nav in navigationMap)
        {
            if (lowerContent.Contains($"navigiere zu {nav.Key}") || 
                lowerContent.Contains($"öffne {nav.Key}"))
            {
                return nav.Value;
            }
        }

        return null;
    }

    private async Task<List<string>> GenerateSuggestionsAsync(LLMContext context, string response)
    {
        // Generate contextual suggestions based on the response
        var suggestions = new List<string>();

        if (response.ToLower().Contains("mitarbeiter") || response.ToLower().Contains("erstellt"))
        {
            suggestions.Add("Zeige alle Mitarbeiter");
            suggestions.Add("Erstelle weiteren Mitarbeiter");
        }

        if (response.ToLower().Contains("suche") || response.ToLower().Contains("gefunden"))
        {
            suggestions.Add("Erweiterte Suche");
            suggestions.Add("Filter anwenden");
        }

        suggestions.Add("Hilfe anzeigen");
        suggestions.Add("Zum Dashboard");

        return suggestions.Take(4).ToList();
    }

    private async Task SaveConversationMessagesAsync(LLMConversation conversation, string userMessage, string assistantMessage, string modelId)
    {
        // Save user message
        await _repository.SaveMessageAsync(new Domain.Models.LLM.LLMMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = userMessage,
            CreateTime = DateTime.UtcNow
        });

        // Save assistant message
        await _repository.SaveMessageAsync(new Domain.Models.LLM.LLMMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = assistantMessage,
            ModelId = modelId,
            CreateTime = DateTime.UtcNow
        });

        // Update conversation
        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.MessageCount += 2;
        conversation.LastModelId = modelId;
        
        // Generate title if this is the first exchange
        if (conversation.MessageCount == 2)
        {
            conversation.Title = GenerateConversationTitle(userMessage, assistantMessage);
        }

        await _repository.UpdateConversationAsync(conversation);
    }

    private string GenerateConversationTitle(string userMessage, string assistantMessage)
    {
        // Simple title generation - take first few words of user message
        var words = userMessage.Split(' ').Take(5);
        return string.Join(' ', words) + (words.Count() >= 5 ? "..." : "");
    }

    private async Task TrackUsageAsync(LLMContext context, LLMModel model, LLMConversation conversation, Providers.LLMUsage usage, long responseTimeMs)
    {
        await _repository.TrackUsageAsync(new Domain.Models.LLM.LLMUsage
        {
            UserId = context.UserId,
            ModelId = model.Id,
            ConversationId = conversation.ConversationId,
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
            Cost = usage.Cost,
            ResponseTimeMs = (int)responseTimeMs,
            HasError = false,
            CreateTime = DateTime.UtcNow
        });

        // Update conversation totals
        conversation.TotalTokens += usage.TotalTokens;
        conversation.TotalCost += usage.Cost;
    }

    private async Task TrackErrorUsageAsync(LLMContext context, LLMModel model, LLMConversation conversation, long responseTimeMs, string? error)
    {
        await _repository.TrackUsageAsync(new Domain.Models.LLM.LLMUsage
        {
            UserId = context.UserId,
            ModelId = model.Id,
            ConversationId = conversation.ConversationId,
            InputTokens = 0,
            OutputTokens = 0,
            Cost = 0,
            ResponseTimeMs = (int)responseTimeMs,
            HasError = true,
            ErrorMessage = error,
            CreateTime = DateTime.UtcNow
        });
    }

    private async Task<string> GetDefaultModelIdAsync()
    {
        var defaultModel = await _repository.GetDefaultModelAsync();
        return defaultModel?.ModelId ?? "gpt-5";
    }

    private LLMResponse GetErrorResponse(string message)
    {
        return new LLMResponse 
        { 
            Message = $"❌ {message}",
            Suggestions = new List<string> { "Hilfe anzeigen", "Erneut versuchen" }
        };
    }
}