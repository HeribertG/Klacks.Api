using MediatR;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Services.LLM;
using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Application.Commands.LLM;

public class ProcessLLMMessageCommand : IRequest<LLMResponse>
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? ModelId { get; set; }
    public List<string> UserRights { get; set; } = new();
}

public class ProcessLLMMessageCommandHandler : IRequestHandler<ProcessLLMMessageCommand, LLMResponse>
{
    private readonly ILLMService _llmService;

    public ProcessLLMMessageCommandHandler(ILLMService llmService)
    {
        _llmService = llmService;
    }

    public async Task<LLMResponse> Handle(ProcessLLMMessageCommand request, CancellationToken cancellationToken)
    {
        var context = new LLMContext
        {
            Message = request.Message,
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            UserRights = request.UserRights,
            AvailableFunctions = GetAvailableFunctions(request.UserRights)
        };

        return await _llmService.ProcessAsync(context);
    }

    private List<LLMFunction> GetAvailableFunctions(List<string> userRights)
    {
        var functions = new List<LLMFunction>();
        
        functions.Add(LLMFunctions.GetSystemInfo);
        functions.Add(LLMFunctions.NavigateToPage);
        
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