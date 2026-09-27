// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thrown when a learning run could not confirm that a skill's description was put back: the knowledge index
/// does not show it, or writing it back failed (then the cause is the inner exception). The run stops here: a
/// description nothing measured may still be what retrieval searches.
/// </summary>
/// <param name="skillName">The skill whose restore was not confirmed</param>
/// <param name="innerException">The failure of the write-back, when it failed</param>
namespace Klacks.Api.Domain.Exceptions;

public class SkillIndexNotRestoredException : Exception
{
    private const string MessageFormat =
        "The knowledge index does not show the restored description of skill '{0}'; the learning run was aborted.";

    private const string WriteBackFailedMessageFormat =
        "Putting back the description of skill '{0}' failed; the learning run was aborted.";

    public SkillIndexNotRestoredException(string skillName)
        : base(string.Format(System.Globalization.CultureInfo.InvariantCulture, MessageFormat, skillName))
    {
        SkillName = skillName;
    }

    public SkillIndexNotRestoredException(string skillName, Exception innerException)
        : base(
            string.Format(System.Globalization.CultureInfo.InvariantCulture, WriteBackFailedMessageFormat, skillName),
            innerException)
    {
        SkillName = skillName;
    }

    public string SkillName { get; }
}
