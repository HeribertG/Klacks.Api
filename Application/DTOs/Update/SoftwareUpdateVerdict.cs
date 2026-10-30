// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Update;

/// <summary>
/// The verdict of the software update advisor. NotRecommended means the trigger would accept the update but the advice
/// is against it; CannotStart means the trigger would refuse.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SoftwareUpdateVerdict>))]
public enum SoftwareUpdateVerdict
{
    StartNow = 0,
    StartWithCaution = 1,
    NotRecommended = 2,
    CannotStart = 3,
    NothingToInstall = 4
}
