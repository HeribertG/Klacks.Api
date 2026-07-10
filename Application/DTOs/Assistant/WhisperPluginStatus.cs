// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Status of the self-hosted Whisper plugin for the settings card: whether the provider is installed
/// and enabled, which model it uses, and the active/last whisper operation from the hand-off queue.
/// OtherOperationActive signals that a non-whisper operation (app update/rollback) currently blocks
/// the single-active queue.
/// </summary>
namespace Klacks.Api.Application.DTOs.Assistant;

public class WhisperPluginStatus
{
    public bool Installed { get; set; }

    public string? ModelAlias { get; set; }

    public string? ModelId { get; set; }

    public WhisperPluginOperationInfo? ActiveOperation { get; set; }

    public WhisperPluginOperationInfo? LastOperation { get; set; }

    public bool OtherOperationActive { get; set; }
}
