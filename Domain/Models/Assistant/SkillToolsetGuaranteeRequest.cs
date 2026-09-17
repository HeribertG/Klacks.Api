// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Input to ISkillToolsetGuaranteeResolver.ResolveAsync: everything a turn's deterministic skill
/// guarantees are computed from.
/// </summary>
/// <param name="AgentId">Agent whose pending-notes count gates the pending-notes guarantee.</param>
/// <param name="PermittedSkills">Skills the user may use; the instances every guarantee resolves against.</param>
/// <param name="RetrievedSkills">Already-retrieved skills, read only to gate the create_shift/cut_shift pair.</param>
/// <param name="UserMessage">Current user message driving every keyword-shaped guarantee.</param>
/// <param name="ConversationId">Conversation used for recipe resumption and the planning-profile draft scope.</param>
/// <param name="CurrentRoute">Current UI route for the page-explain guarantee.</param>
/// <param name="UserId">User whose pending notes, proposal hints and planning-profile draft gate their guarantees.</param>
/// <param name="Language">UI language used for engine-recipe matching.</param>
/// <param name="UserRights">Permissions the engine-recipe guarantee scopes its match against.</param>
/// <param name="PinnedSkillNames">Skills a prior clarification question offered, guaranteed regardless of retrieval.</param>
/// <param name="ApplyLearnedPhraseGuarantee">Whether a learning-loop wording may claim a guarantee slot.</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillToolsetGuaranteeRequest(
    Guid AgentId,
    IReadOnlyList<AgentSkill> PermittedSkills,
    IReadOnlyList<AgentSkill> RetrievedSkills,
    string UserMessage,
    string? ConversationId,
    string? CurrentRoute,
    string UserId,
    string? Language,
    List<string> UserRights,
    IReadOnlyCollection<string>? PinnedSkillNames,
    bool ApplyLearnedPhraseGuarantee);
