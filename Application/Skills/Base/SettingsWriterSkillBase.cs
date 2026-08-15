// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared write path for skills that change entries in the settings store. Collects the supplied
/// values, writes each one as an insert or an update, commits once, and reads the keys back to
/// confirm the store really holds them — a skill that reports success without that check would be
/// claiming a persistence it never verified.
/// </summary>
/// <param name="settingsRepository">Reads and writes the individual setting rows</param>
/// <param name="unitOfWork">Commits the collected changes in one go</param>
/// <param name="encryptionService">Encrypts sensitive values before they reach the store and decrypts
/// them again for the read-back check</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.Base;

public abstract class SettingsWriterSkillBase : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISettingsEncryptionService _encryptionService;

    protected SettingsWriterSkillBase(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ISettingsEncryptionService encryptionService)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
    }

    /// <summary>
    /// Writes the collected key/value pairs, commits, and verifies each key by reading it back.
    /// </summary>
    /// <param name="pending">Setting key to new value, keyed by the parameter name reported back.</param>
    /// <param name="subject">What the message calls the changed group, for example "overtime settings".</param>
    protected async Task<SkillResult> PersistAsync(
        IReadOnlyList<PendingSetting> pending,
        string subject)
    {
        if (pending.Count == 0)
        {
            return SkillResult.Error($"No parameters supplied — {subject} left unchanged.");
        }

        foreach (var setting in pending)
        {
            await UpsertSettingAsync(setting.Key, setting.Value);
        }

        await _unitOfWork.CompleteAsync();

        var unconfirmed = new List<string>();
        foreach (var setting in pending)
        {
            var persisted = await _settingsRepository.GetSetting(setting.Key);
            var persistedValue = persisted == null
                ? null
                : _encryptionService.ProcessForReading(setting.Key, persisted.Value ?? string.Empty);

            if (persistedValue != setting.Value)
            {
                unconfirmed.Add(setting.ParameterName);
            }
        }

        if (unconfirmed.Count > 0)
        {
            return SkillResult.Error(
                $"Database verification failed for: {string.Join(", ", unconfirmed)} — " +
                "treat the change as not persisted.");
        }

        var changed = pending.Select(p => p.ParameterName).ToList();

        return SkillResult.SuccessResult(
            new { ChangedFields = changed },
            $"{subject} updated ({string.Join(", ", changed)}) and confirmed in the database (verified).");
    }

    /// <summary>
    /// Adds a text parameter to the pending list when it was supplied. Values are stored as text
    /// throughout the settings store, so the typed variants below only differ in how they parse.
    /// </summary>
    protected static void CollectText(
        List<PendingSetting> pending,
        Dictionary<string, object> parameters,
        string parameterName,
        string key)
    {
        var value = GetParameter<string>(parameters, parameterName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            pending.Add(new PendingSetting(parameterName, key, value.Trim()));
        }
    }

    protected static void CollectBoolean(
        List<PendingSetting> pending,
        Dictionary<string, object> parameters,
        string parameterName,
        string key)
    {
        var value = GetParameter<bool?>(parameters, parameterName);
        if (value.HasValue)
        {
            pending.Add(new PendingSetting(parameterName, key, value.Value ? "true" : "false"));
        }
    }

    protected static void CollectInteger(
        List<PendingSetting> pending,
        Dictionary<string, object> parameters,
        string parameterName,
        string key)
    {
        var value = GetParameter<int?>(parameters, parameterName);
        if (value.HasValue)
        {
            pending.Add(new PendingSetting(
                parameterName, key, value.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    protected static void CollectDecimal(
        List<PendingSetting> pending,
        Dictionary<string, object> parameters,
        string parameterName,
        string key)
    {
        var value = GetParameter<decimal?>(parameters, parameterName);
        if (value.HasValue)
        {
            pending.Add(new PendingSetting(
                parameterName, key, value.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private async Task UpsertSettingAsync(string key, string value)
    {
        var storedValue = _encryptionService.ProcessForStorage(key, value);

        var existing = await _settingsRepository.GetSetting(key);
        if (existing != null)
        {
            existing.Value = storedValue;
            await _settingsRepository.PutSetting(existing);
            return;
        }

        await _settingsRepository.AddSetting(new Domain.Models.Settings.Settings
        {
            Id = Guid.NewGuid(),
            Type = key,
            Value = storedValue
        });
    }
}
