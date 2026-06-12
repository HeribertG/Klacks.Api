// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Converts the permission-filtered skill registry into MCP tool definitions, including JSON
/// input schemas and risk-based tool annotations (read-only / destructive hints).
/// </summary>
/// <param name="userPermissions">Role permissions of the authenticated user used to filter the registry</param>

using System.Text.Json;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Adapters;
using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public class McpToolCatalog : IMcpToolCatalog
{
    private static readonly JsonSerializerOptions SchemaSerializerOptions = new();

    private readonly ISkillRegistry _skillRegistry;
    private readonly IMcpSkillExposurePolicy _exposurePolicy;
    private readonly ISkillRiskClassifier _riskClassifier;

    public McpToolCatalog(
        ISkillRegistry skillRegistry,
        IMcpSkillExposurePolicy exposurePolicy,
        ISkillRiskClassifier riskClassifier)
    {
        _skillRegistry = skillRegistry;
        _exposurePolicy = exposurePolicy;
        _riskClassifier = riskClassifier;
    }

    public IList<Tool> GetToolsForUser(IReadOnlyList<string> userPermissions)
    {
        return _skillRegistry.GetSkillsForUser(userPermissions)
            .Where(_exposurePolicy.IsExposed)
            .OrderBy(descriptor => descriptor.Name, StringComparer.Ordinal)
            .Select(ToTool)
            .ToList();
    }

    private Tool ToTool(SkillDescriptor descriptor)
    {
        var inputSchema = SkillParameterSchemaBuilder.BuildInputSchema(descriptor);

        return new Tool
        {
            Name = descriptor.Name,
            Description = descriptor.Description,
            InputSchema = JsonSerializer.SerializeToElement(inputSchema, SchemaSerializerOptions),
            Annotations = BuildAnnotations(_riskClassifier.Classify(descriptor))
        };
    }

    private static ToolAnnotations BuildAnnotations(SkillRiskClass riskClass)
    {
        return riskClass switch
        {
            SkillRiskClass.ReadOnly => new ToolAnnotations { ReadOnlyHint = true },
            SkillRiskClass.Reversible => new ToolAnnotations { ReadOnlyHint = false, DestructiveHint = false },
            SkillRiskClass.ScenarioGated => new ToolAnnotations { ReadOnlyHint = false, DestructiveHint = false },
            _ => new ToolAnnotations { ReadOnlyHint = false, DestructiveHint = true }
        };
    }
}
