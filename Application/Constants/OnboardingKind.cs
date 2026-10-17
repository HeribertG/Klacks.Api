// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Which Klacksy tour an onboarding slice or save request is about: the installation-wide setup tour of the admin or
/// the short per-account intro tour every user gets.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class OnboardingKind
{
    public const string Setup = "setup";
    public const string Intro = "intro";
}
