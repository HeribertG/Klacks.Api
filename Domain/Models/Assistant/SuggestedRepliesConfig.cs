// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class SuggestedRepliesConfig
{
    public string SelectionMode { get; set; } = SuggestedReplySelectionModes.Single;
    public string? Prompt { get; set; }
    public List<SuggestedReply> Options { get; set; } = [];
}
