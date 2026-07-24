// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Size limit constants for blob audio uploads to the STT transcription endpoint.
/// The 25 MB cap mirrors the per-file upload limit of the Whisper-style transcription
/// providers (OpenAI and Groq Whisper accept at most 25 MB per audio file).
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class SttUploadConstants
{
    private const long BytesPerMegabyte = 1_000_000L;

    public const int MaxAudioUploadSizeMegabytes = 25;
    public const long MaxAudioUploadSizeBytes = MaxAudioUploadSizeMegabytes * BytesPerMegabyte;

    public static readonly string AudioTooLargeMessage =
        $"Audio upload exceeds the maximum allowed size of {MaxAudioUploadSizeMegabytes} MB.";
}
