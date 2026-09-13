// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Settings of the post-turn auto memory extraction, bound to the configuration section
/// <c>Assistant:AutoMemory</c>.
/// </summary>
/// <param name="Enabled">Whether a finished chat turn is analysed for durable facts at all</param>
/// <param name="ContextTtlDays">How long a memory of a situational category stays valid before it expires</param>
namespace Klacks.Api.Domain.Services.Assistant;

public class AutoMemoryOptions
{
    public const string SectionName = "Assistant:AutoMemory";

    public bool Enabled { get; set; } = true;

    public int ContextTtlDays { get; set; } = 30;
}
