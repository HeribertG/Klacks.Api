// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Navigates the Klacks UI to the schedule "Send to client" dialog for a given client and
/// period. The actual PDF generation runs on the frontend via SendScheduleReportCommand
/// (which expects PDF bytes from the rendered schedule), so this skill returns a navigate
/// UiAction rather than performing the email server-side.
/// </summary>
/// <param name="clientId">Required. UUID of the client whose schedule should be emailed.</param>
/// <param name="fromDate">Optional. ISO date yyyy-MM-dd.</param>
/// <param name="untilDate">Optional. ISO date yyyy-MM-dd.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("email_schedule_to_client")]
public class EmailScheduleToClientSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;

    public EmailScheduleToClientSkill(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var fromDate = GetParameter<string>(parameters, "fromDate");
        var untilDate = GetParameter<string>(parameters, "untilDate");

        var client = await _clientRepository.Get(clientId);
        if (client == null)
        {
            return SkillResult.Error($"Client '{clientId}' not found.");
        }

        var queryParts = new List<string> { $"clientId={clientId}" };
        if (!string.IsNullOrWhiteSpace(fromDate)) queryParts.Add($"from={fromDate}");
        if (!string.IsNullOrWhiteSpace(untilDate)) queryParts.Add($"until={untilDate}");
        var route = "/schedule?" + string.Join('&', queryParts);

        return SkillResult.Navigation(
            new
            {
                Route = route,
                ClientId = clientId,
                ClientName = $"{client.FirstName} {client.Name}".Trim(),
                FromDate = fromDate,
                UntilDate = untilDate,
                Target = "schedule-send-to-client-modal"
            },
            $"Navigated to the schedule send-to-client dialog for {client.FirstName} {client.Name}. Please confirm in the UI.");
    }
}
