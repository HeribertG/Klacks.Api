// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Installs and uninstalls the user-facing skill labels a language plugin contributes.
/// </summary>
/// <param name="pluginDirectory">Base directory of the language plugins</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class LanguagePluginSkillLabelInstaller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _pluginDirectory;
    private readonly ILogger _logger;

    public LanguagePluginSkillLabelInstaller(
        string pluginDirectory,
        ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }

    /// <summary>
    /// Writes the pack's user-facing skill labels into the enabled skills it names. The mirror image of
    /// LanguagePluginContentInstaller.InstallSkillSynonymsAsync and deliberately simpler: a synonym list
    /// is merged phrase by phrase against the skill_phrase table because a user may have added phrases of
    /// their own, while a label is ONE authored string per language that nobody else writes, so the pack
    /// simply owns its key. Nothing is mirrored into skill_phrase: synonyms are input vocabulary that
    /// decides whether a skill is selected, labels are output text that names it back to the user, and a
    /// label in the matching path would change routing.
    /// A label that is already stored under this code is skipped without a write. IAgentSkillRepository
    /// .UpdateAsync commits on its own, so one unconditional write per named skill would mean one round
    /// trip per skill per pack on EVERY startup once the backfill runs - 21 packs times the seeded skills,
    /// re-writing values that did not change. The synonym twin gets away without this because its merge
    /// against skill_phrase is per-phrase work the row genuinely needs; a label is one string.
    /// </summary>
    /// <param name="scope">Scope providing the skill repository</param>
    /// <param name="code">Language code of the pack, used verbatim as the label key</param>
    /// <param name="onlySkillNames">When set, only these skills are updated; null updates every enabled skill</param>
    public async Task InstallSkillLabelsAsync(
        IServiceScope scope, string code, IReadOnlyCollection<string>? onlySkillNames = null)
    {
        var labelsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.SkillLabelsFileName);
        if (!File.Exists(labelsPath))
            return;

        try
        {
            var labelMap = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(labelsPath), JsonOptions);
            if (labelMap == null || labelMap.Count == 0)
                return;

            var skillFilter = onlySkillNames == null
                ? null
                : new HashSet<string>(onlySkillNames, StringComparer.OrdinalIgnoreCase);

            var skillRepo = scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>();
            var allSkills = await skillRepo.GetAllEnabledTrackedAsync();
            var count = 0;

            foreach (var skill in allSkills)
            {
                if (skillFilter != null && !skillFilter.Contains(skill.Name))
                    continue;

                if (!labelMap.TryGetValue(skill.Name, out var label) || string.IsNullOrWhiteSpace(label))
                    continue;

                skill.Labels ??= new Dictionary<string, string>();
                if (skill.Labels.TryGetValue(code, out var current)
                    && string.Equals(current, label.Trim(), StringComparison.Ordinal))
                {
                    continue;
                }

                skill.Labels[code] = label.Trim();
                await skillRepo.UpdateAsync(skill);
                count++;
            }

            _logger.LogInformation(
                "Installed skill labels for language plugin '{Code}': {Count} skill(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install skill labels for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Removes the pack's labels again. Column-driven, not file-driven: it walks the rows and drops the
    /// pack's own key, so a pack whose file was renamed or deleted between install and uninstall still
    /// cleans up after itself. That is the newer precedent set by
    /// LanguagePluginContentInstaller.UninstallRecipeVetoesAsync; the file-driven skill- and
    /// recipe-synonym uninstallers predate it and are not copied here.
    /// </summary>
    /// <param name="scope">Scope providing the skill repository</param>
    /// <param name="code">Language code of the pack whose labels are dropped</param>
    public async Task UninstallSkillLabelsAsync(IServiceScope scope, string code)
    {
        try
        {
            var skillRepo = scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>();
            var allSkills = await skillRepo.GetAllEnabledTrackedAsync();
            var count = 0;

            foreach (var skill in allSkills)
            {
                if (skill.Labels == null || !skill.Labels.Remove(code))
                    continue;

                await skillRepo.UpdateAsync(skill);
                count++;
            }

            _logger.LogInformation(
                "Uninstalled skill labels for language plugin '{Code}': {Count} skill(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall skill labels for language plugin '{Code}'", code.ForLog());
        }
    }
}
