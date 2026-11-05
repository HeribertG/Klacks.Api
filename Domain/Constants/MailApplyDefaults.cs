// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class MailApplyDefaults
{
    public const string EntryTurnNote =
        "[ENTERING DATA FROM E-MAILS — The user wants what the e-mails ask for entered now. Use what the mails say; " +
        "do not ask for it again and do not offer a guided flow. Name each person by first and last name directly in the write " +
        "skill. Availability (days and hours someone can or cannot work) goes to set_client_availability; wishes as key " +
        "commands (FREE, NIGHT, ...) go to add_schedule_command or add_schedule_commands_range. Put EVERY call of the same " +
        "skill into ONE tool-call batch: that skill is refused when it is called again in a later round of this turn. Only " +
        "claim what the results confirm.]";
}
