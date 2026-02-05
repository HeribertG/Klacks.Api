using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Presentation.DTOs.LLM;

public class LLMFunctionExecuteRequest
{
    [Required]
    public string FunctionName { get; set; } = string.Empty;

    public Dictionary<string, object>? Parameters { get; set; }
}
