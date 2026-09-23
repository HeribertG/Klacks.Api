// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Converts the permission-filtered skill registry into MCP tool definitions, including JSON
/// input schemas and risk-based tool annotations (read-only / destructive hints). Skills whose results carry
/// externally authored content (UntrustedSkillOutputs) additionally get the open-world hint; for all other
/// tools it stays unset, because the MCP default for an unset hint is "open world" and claiming a closed world
/// would be unverified for skills that reach external systems.
/// </summary>
/// <param name="userPermissions">Role permissions of the authenticated user used to filter the registry</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
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
            Annotations = BuildAnnotations(_riskClassifier.Classify(descriptor), descriptor.Name)
        };
    }

    private static ToolAnnotations BuildAnnotations(SkillRiskClass riskClass, string skillName)
    {
        bool? openWorldHint = UntrustedSkillOutputs.Contains(skillName) ? true : null;

        return riskClass switch
        {
            SkillRiskClass.ReadOnly => new ToolAnnotations { ReadOnlyHint = true, OpenWorldHint = openWorldHint },
            SkillRiskClass.Reversible => new ToolAnnotations
            {
                ReadOnlyHint = false, DestructiveHint = false, OpenWorldHint = openWorldHint
            },
            SkillRiskClass.ScenarioGated => new ToolAnnotations
            {
                ReadOnlyHint = false, DestructiveHint = false, OpenWorldHint = openWorldHint
            },
            _ => new ToolAnnotations { ReadOnlyHint = false, DestructiveHint = true, OpenWorldHint = openWorldHint }
        };
    }
}
