using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.AI;

public class LlmFunctionDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ParametersJson { get; set; } = "[]";
    public string? RequiredPermission { get; set; }
    public string ExecutionType { get; set; } = "BuiltIn";
    public string Category { get; set; } = "backend";
    public bool IsEnabled { get; set; } = true;
    public int SortOrder { get; set; }
}
