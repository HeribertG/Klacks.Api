// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ITurnConfirmationScope
{
    void MarkIssuedForSensitiveSkill(string token);

    bool WasIssuedThisTurnForSensitiveSkill(string token);
}
