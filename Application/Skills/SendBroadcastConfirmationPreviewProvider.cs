// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of send_broadcast (messaging plugin): when the autonomy gate holds the call, this provider asks the very
/// planner the skill uses, with the caller's own admin right, and shows how many people it reaches, how, on which channel, who is
/// left out and the text verbatim; never an address. It refuses, so the gate issues no token, what the skill would refuse (missing
/// parameter, unknown or disabled provider, empty or hidden audience, nobody with an address, everybody already served). The facts
/// are returned as the preview's binding (SendBroadcastBinding) and stored with the token; the confirmed call sends exactly these
/// or nothing. It only reads. It lives in the host because the plugin contract has no preview hook, like the one of send_message.
/// </summary>
/// <param name="planner">Computes the plan exactly like the skill that sends</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Plugin.Messaging.Application.Constants;
using Klacks.Plugin.Messaging.Application.Interfaces;
using Klacks.Plugin.Messaging.Application.Models;
using Klacks.Plugin.Messaging.Application.Services;

namespace Klacks.Api.Application.Skills;

public class SendBroadcastConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private const string BoundNote =
        "Exactly these recipients, this channel and this text are bound to the confirmation: if a recipient or the text " +
        "differs when the user confirms, nothing is sent and a fresh preview is shown.";

    private readonly IBroadcastPlanner _planner;

    public SendBroadcastConfirmationPreviewProvider(IBroadcastPlanner planner)
    {
        _planner = planner;
    }

    public bool Supports(string skillName) =>
        string.Equals(skillName, MessagingConstants.SkillSendBroadcast, StringComparison.OrdinalIgnoreCase);

    public async Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        SendBroadcastParameters request;
        try
        {
            request = SendBroadcastParameters.Parse(parameters);
        }
        catch (ArgumentException ex)
        {
            return SkillConfirmationPreview.Refuse(ex.Message);
        }

        var isAdmin = context.UserPermissions.Contains(Roles.Admin);
        var plan = await _planner.PlanAsync(request, isAdmin, cancellationToken);
        if (!plan.Success)
        {
            return SkillConfirmationPreview.Refuse(plan.Error!);
        }

        var recipients = plan.Recipients!;
        var viaPhone = recipients.Recipients.Count(recipient => recipient.FromPhoneFallback);
        var viaContact = recipients.Recipients.Count - viaPhone;
        var text =
            $"Send a broadcast via {recipients.ProviderName} to {recipients.Recipients.Count} recipient(s): {viaContact} by messenger " +
            $"contact, {viaPhone} by mobile number. Left out: {recipients.Skipped} without an address, {plan.AlreadyReceived} who " +
            $"received this text within the last {MessagingConstants.BroadcastRepeatWindowMinutes} minutes. The text, verbatim:\n<<<\n" +
            $"{request.Content}\n>>>\n{BoundNote}";
        var binding = ConfirmationBinding.Of(
            ConfirmationBindingKinds.Broadcast, SendBroadcastBinding.ItemsFor(recipients, request.Content));
        return SkillConfirmationPreview.Show(text, binding);
    }
}
