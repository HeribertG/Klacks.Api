// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single source of truth for the name prefixes that identify read-only (non-mutating) skills.
/// Two consumers depend on it: the LLM multi-turn loop, which lets read-only calls repeat inside one
/// turn while a side-effecting skill may run only once, and SkillRiskClassifier, which uses the same
/// prefixes as the last fallback after its category check. Keeping the prefixes in one place stops the
/// two lists from drifting apart, which they previously did.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ReadOnlySkillPrefixes
{
    public const string Get = "get_";

    public const string List = "list_";

    public const string Search = "search_";

    public const string Find = "find_";

    public const string Read = "read_";

    public const string Lookup = "lookup_";

    public const string Verify = "verify_";

    public const string Check = "check_";

    public const string Detect = "detect_";

    public const string Interpret = "interpret_";

    public const string Validate = "validate_";

    public const string Test = "test_";

    public const string Evaluate = "evaluate_";

    public const string Generate = "generate_";

    public static readonly IReadOnlyList<string> All =
    [
        Get, List, Search, Find, Read, Lookup, Verify, Check,
        Detect, Interpret, Validate, Test, Evaluate, Generate
    ];

    public static bool HasReadOnlyPrefix(string? skillName)
    {
        if (string.IsNullOrEmpty(skillName))
        {
            return false;
        }

        foreach (var prefix in All)
        {
            if (skillName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
