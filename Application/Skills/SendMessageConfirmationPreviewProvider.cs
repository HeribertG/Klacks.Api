// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirmation preview of send_message (messaging plugin): when the autonomy gate holds the call, this provider resolves the
/// recipient with the very resolver the skill uses (IMessageRecipientResolver, with the caller's own admin right, so a
/// non-admin sees only the contacts of clients its group visibility allows) and shows the channel, the resolved recipient
/// (display name and address) and the text verbatim. It refuses - so the gate issues no token - what the skill would refuse
/// (unknown or hidden contact, ambiguous name, owner or application user for a non-admin, missing parameter). The facts it shows
/// are returned as the preview's binding (SendMessageBinding: provider, address, content type, hash of the text) and stored with
/// the token; the confirmed call sends exactly these or nothing, so an injected instruction in a received message cannot make the
/// user confirm one thing and send another. It only reads. The user sees these facts only in the model's wording.
/// </summary>
/// <param name="recipientResolver">Resolves the call's recipient exactly like the skill that sends</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Plugin.Messaging.Application.Constants;
using Klacks.Plugin.Messaging.Application.Interfaces;
using Klacks.Plugin.Messaging.Application.Models;
using Klacks.Plugin.Messaging.Application.Services;

namespace Klacks.Api.Application.Skills;

public class SendMessageConfirmationPreviewProvider : ISkillConfirmationPreviewProvider
{
    private const string BoundNote =
        "Exactly this channel, recipient address and text are bound to the confirmation: if the recipient or the text " +
        "differs when the user confirms, nothing is sent and a fresh preview is shown.";

    private readonly IMessageRecipientResolver _recipientResolver;

    public SendMessageConfirmationPreviewProvider(IMessageRecipientResolver recipientResolver)
    {
        _recipientResolver = recipientResolver;
    }

    public bool Supports(string skillName) =>
        string.Equals(skillName, MessagingConstants.SkillSendMessage, StringComparison.OrdinalIgnoreCase);

    public async Task<SkillConfirmationPreview> BuildAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        SendMessageParameters request;
        try
        {
            request = SendMessageParameters.Parse(parameters);
        }
        catch (ArgumentException ex)
        {
            return SkillConfirmationPreview.Refuse(ex.Message);
        }

        var isAdmin = context.UserPermissions.Contains(Roles.Admin);
        var resolution = await _recipientResolver.ResolveAsync(request, isAdmin, cancellationToken);
        if (!resolution.Success)
        {
            return SkillConfirmationPreview.Refuse(resolution.Error!);
        }

        var text =
            $"Send a {request.ContentType} message via {resolution.ProviderName} to {resolution.DisplayName} " +
            $"({resolution.Identifier}). The text, verbatim:\n<<<\n{request.Content}\n>>>\n{BoundNote}";
        var binding = ConfirmationBinding.Of(
            ConfirmationBindingKinds.Message,
            SendMessageBinding.ItemsFor(resolution, request.Content, request.ContentType));
        return SkillConfirmationPreview.Show(text, binding);
    }
}
