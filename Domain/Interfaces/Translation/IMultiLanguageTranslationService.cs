// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Interfaces.Translation;

public interface IMultiLanguageTranslationService
{
    Task<MultiLanguage> TranslateEmptyFieldsAsync(MultiLanguage multiLanguage);
    bool IsConfigured { get; }
}
