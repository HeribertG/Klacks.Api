// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

using Klacks.Api.Domain.Models.Assistant;

public interface IPhoneticEncoderFactory
{
    IPhoneticEncoder Create(PhoneticConfig config);
}
