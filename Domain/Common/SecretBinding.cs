// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Common;

public record SecretBinding(string SettingType, string? ProvidedValue);
