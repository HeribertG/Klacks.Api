using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Services.LLM;
using Klacks.Api.Domain.Services.Skills;
using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Application.Commands.LLM;

public class ProcessLLMMessageCommand : IRequest<LLMResponse>
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? ModelId { get; set; }
    public string? Language { get; set; }
    public List<string> UserRights { get; set; } = new();
}

public class ProcessLLMMessageCommandHandler : IRequestHandler<ProcessLLMMessageCommand, LLMResponse>
{
    private readonly ILLMService _llmService;
    private readonly ILLMSkillBridge _skillBridge;

    public ProcessLLMMessageCommandHandler(ILLMService llmService, ILLMSkillBridge skillBridge)
    {
        _llmService = llmService;
        _skillBridge = skillBridge;
    }

    public async Task<LLMResponse> Handle(ProcessLLMMessageCommand request, CancellationToken cancellationToken)
    {
        var context = new LLMContext
        {
            Message = request.Message,
            UserId = request.UserId,
            ConversationId = request.ConversationId,
            ModelId = request.ModelId,
            Language = request.Language,
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
            functions.Add(LLMFunctions.SearchAndNavigate);
        }

        if (userRights.Contains("CanCreateClients") || userRights.Contains("Admin"))
        {
            functions.Add(LLMFunctions.CreateClient);
        }

        if (userRights.Contains("CanCreateContracts") || userRights.Contains("Admin"))
        {
            functions.Add(LLMFunctions.CreateContract);
        }

        if (userRights.Contains("CanEditSettings") || userRights.Contains("Admin"))
        {
            functions.Add(LLMFunctions.CreateSystemUser);
            functions.Add(LLMFunctions.DeleteSystemUser);
            functions.Add(LLMFunctions.ListSystemUsers);
            functions.Add(LLMFunctions.CreateBranch);
            functions.Add(LLMFunctions.DeleteBranch);
            functions.Add(LLMFunctions.ListBranches);
            functions.Add(LLMFunctions.CreateMacro);
            functions.Add(LLMFunctions.DeleteMacro);
            functions.Add(LLMFunctions.ListMacros);
        }

        var skillFunctions = _skillBridge.GetSkillsAsLLMFunctions(userRights);
        var existingNames = new HashSet<string>(functions.Select(f => f.Name));
        foreach (var skillFunction in skillFunctions)
        {
            if (!existingNames.Contains(skillFunction.Name))
            {
                functions.Add(skillFunction);
            }
        }

        return functions;
    }
}