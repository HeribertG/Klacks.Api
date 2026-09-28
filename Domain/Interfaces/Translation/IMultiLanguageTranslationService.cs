// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Interfaces.Translation;

public interface IMultiLanguageTranslationService
{
    Task<MultiLanguage> TranslateEmptyFieldsAsync(MultiLanguage multiLanguage);
    Task<bool> IsConfiguredAsync();
}
