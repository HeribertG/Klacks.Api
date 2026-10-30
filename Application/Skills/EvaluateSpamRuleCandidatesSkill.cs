// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Advises which sender domains keep landing in the junk folder without a spam rule: per domain the number of junk mails
/// and senders, how many mails of the same domain sit in the inbox and client folders, and which recurring domains are
/// left out because a rule already catches them (checked with the spam filter's own matcher), is switched off, or belongs
/// to the own mailbox or a known contact. Only host names and counts leave, never a subject, a body or a display name.
/// The folder read is the junk folder alone; reading is cut at SpamRuleCandidateLimits.MaxJunkSenderRows sender addresses
/// and the result says so. Creating a rule is a separate step the system confirms once; this skill changes nothing.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Handlers.Email;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Email;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class EvaluateSpamRuleCandidatesSkill : BaseSkillImplementation
{
    public const string SkillName = "evaluate_spam_rule_candidates";

    private static readonly string[] OwnMailboxSettingKeys =
    [
        Settings.APP_EMAIL_ADDRESS,
        Settings.APP_OUTGOING_SERVER_USERNAME,
        Settings.APP_INCOMING_SERVER_USERNAME
    ];

    private readonly IReceivedEmailRepository _receivedEmailRepository;
    private readonly ISpamRuleRepository _spamRuleRepository;
    private readonly IEmailFolderRepository _emailFolderRepository;
    private readonly IEmailQueryRepository _emailQueryRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ISettingsReader _settingsReader;

    public EvaluateSpamRuleCandidatesSkill(
        IReceivedEmailRepository receivedEmailRepository,
        ISpamRuleRepository spamRuleRepository,
        IEmailFolderRepository emailFolderRepository,
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ISettingsReader settingsReader)
    {
        _receivedEmailRepository = receivedEmailRepository;
        _spamRuleRepository = spamRuleRepository;
        _emailFolderRepository = emailFolderRepository;
        _emailQueryRepository = emailQueryRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _settingsReader = settingsReader;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var junkFolder = await _emailFolderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Junk);
        var rules = await _spamRuleRepository.GetAllAsync();
        var ownDomains = await ReadOwnDomainsAsync(cancellationToken);
        var communications = await _emailQueryRepository.GetClientsWithEmailCommunicationsAsync(cancellationToken);
        var contactDomains = DomainsOf(communications.Select(communication => communication.EmailAddress));

        if (string.IsNullOrEmpty(junkFolder))
        {
            return Relay(SpamRuleCandidateAdvisor.Evaluate(new SpamRuleCandidateFacts(
                false, 0, false, new SenderDomainGrouping([], 0), rules, ownDomains, contactDomains, new Dictionary<string, int>())));
        }

        var hiddenSenders = await ReceivedEmailVisibility.GetHiddenSenderAddressesAsync(
            communications, _clientVisibilityGuard, cancellationToken);
        var junkSenders = await _receivedEmailRepository.GetSenderMailCountsAsync(
            junkFolder, hiddenSenders, SpamRuleCandidateLimits.MaxJunkSenderRows, cancellationToken);
        var grouping = SpamSenderDomainGrouping.Group(junkSenders.Senders);
        var inboxMails = await CountMailsOutsideJunkAsync(grouping, cancellationToken);

        return Relay(SpamRuleCandidateAdvisor.Evaluate(new SpamRuleCandidateFacts(
            true, junkSenders.TotalMailCount, junkSenders.IsTruncated, grouping, rules, ownDomains, contactDomains, inboxMails)));
    }

    private async Task<Dictionary<string, int>> CountMailsOutsideJunkAsync(
        SenderDomainGrouping grouping, CancellationToken cancellationToken)
    {
        var inboxFolder = await _emailFolderRepository.GetImapNameBySpecialUseAsync(FolderSpecialUse.Inbox);
        var folders = new List<string> { EmailConstants.ClientAssignedFolder };
        if (!string.IsNullOrEmpty(inboxFolder))
        {
            folders.Add(inboxFolder);
        }

        var domains = grouping.Groups
            .Where(group => group.MailCount >= SpamRuleCandidateLimits.MinimumJunkMailsPerDomain)
            .Take(SpamRuleCandidateLimits.MaxInboxLookupDomains)
            .Select(group => group.Domain)
            .ToList();

        var counts = await _receivedEmailRepository.GetMailCountsBySenderDomainAsync(folders, domains, cancellationToken);
        return counts.ToDictionary(count => count.Domain, count => count.MailCount, StringComparer.Ordinal);
    }

    private async Task<HashSet<string>> ReadOwnDomainsAsync(CancellationToken cancellationToken)
    {
        var values = await _settingsReader.GetSettingsByTypesAsync(OwnMailboxSettingKeys, cancellationToken);
        return DomainsOf(values.Values);
    }

    private static HashSet<string> DomainsOf(IEnumerable<string> addresses)
    {
        return addresses
            .Select(SpamRuleMatcher.ExtractDomain)
            .OfType<string>()
            .Select(domain => domain.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);
    }

    private static SkillResult Relay(SpamRuleCandidateEvaluation evaluation)
    {
        return SkillResult.SuccessResult(evaluation, evaluation.Recommendation);
    }
}
