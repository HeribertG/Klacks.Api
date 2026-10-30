---
name: explain_messenger_pairing_and_scope
description: |
  Explains how a person gets linked to a messenger and who may write to whom: the one-time Telegram
  invitation of an employee, the pairing code a logged-in user creates for their own account only, the
  "to me" address that means the Klacks owner, the administrator-only recipients and why a supervisor
  reaches only employees of visible groups. Use when somebody asks how to connect Telegram, why a message
  to me, to an employee or to a user is refused, or who sees which messages.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - telegram verknüpfen
    - kopplungscode
    - wer darf wem schreiben
    - nachricht an mich per messenger
    - konto mit telegram koppeln
  en:
    - link telegram account
    - pairing code
    - who may message whom
    - message to me by messenger
    - pair my account with telegram
synonyms:
  de: [wie verknüpfe ich einen mitarbeiter mit telegram, warum darf ich keine nachricht an mich senden, wer sieht welche messenger-nachrichten, eigenes konto mit dem messenger koppeln]
  en: [how do I link an employee to telegram, why can I not send a message to myself, who sees which messenger messages, pair my own account with the messenger]
  fr: [comment lier un employé à telegram, pourquoi ne puis-je pas m'écrire un message, qui voit quels messages, coupler mon compte à la messagerie]
  it: [come collegare un dipendente a telegram, perché non posso scrivere a me stesso, chi vede quali messaggi, collegare il mio account al messenger]
---

# Messenger pairing and scope — how people are linked and who may write to whom

## Core idea (one sentence)

Three kinds of identity are linked, each on its own path — an **employee** by an invitation, a **login account** by a self-issued code, the **owner** by a list an administrator maintains — and what a caller may then read or send is limited by role and by group visibility.

<!-- level:short -->

## Stage 1 — What it is and what it is for

A messenger id such as a Telegram chat id cannot be typed in by anybody, so nobody can simply enter it. The person instead opens a one-time link or sends a one-time code to the bot, and Klacks stores the sender identity it observed. Only a linked identity can receive messages from Klacks and have its replies stored (see `explain_messenger_channels`).

| Who is linked | Path | Done by | Valid for |
|---|---|---|---|
| Employee | invitation e-mail with a Telegram start link, the person presses START | an administrator (single invitation); automatically once to all employees when Telegram is first switched on | 30 days |
| Login account | pairing code, sent to the bot | the user, for their own account only; an administrator can e-mail an invitation to another user | self-issued 15 minutes, administrator invitation 72 hours |
| Owner | list of owner messenger identities in the settings | an administrator | until changed |

<!-- level:elements -->

## Stage 2 — The paths step by step

### Employee (Telegram)

1. Telegram is enabled and reachable by the bot. Only employees are invited.
2. The invitation goes by e-mail to the **private e-mail** of the employee; with only a phone number the invitation fails (an SMS invitation does not exist). An employee who is already linked is skipped, and a new invitation replaces the older one.
3. The employee opens the link on the phone and presses START. The chat id is stored as the employee's Telegram contact; the link works once.
4. Other channels have no invitation: the employee needs a stored messenger contact of that channel with the matching identifier.

### Login account

1. The user creates the pairing code in their own messenger settings. There is no way to create one for another account, and the code is only 8 characters from an alphabet without the look-alikes I, O, 0 and 1.
2. The user sends the code to the Telegram bot (as the start command; the deep link does it in one tap). An administrator can e-mail that link to a user who has not done it.
3. If the sending identity is already linked to **another** account the code is refused; for the **same** account it counts as done without adding a duplicate. The first linked channel becomes the preferred one.
4. At present only Telegram redeems such codes. An expired code is reported as expired, an unknown one as unknown, so ask for a new code in the first case.
5. A user may remove their own paired channel; an administrator may remove any.

### "To me" and the owner

The words me, myself, self and their translations (de: mir, ich) mean the **owner of the Klacks installation**, looked up in the owner list for the type of the chosen channel - not the person who is asking and not an employee. If the list has no entry for that channel, the send is refused and names the owner messenger settings. The list is read and changed by administrators only.

### Who may write to whom

| Caller | Employees | Owner and "to me" | Login accounts | Free address without a known contact | Reads messages |
|---|---|---|---|---|---|
| Administrator | all | yes | yes, over their paired channel (the preferred one if no channel is named) | yes | all, including conversations without an employee |
| Supervisor | only employees of groups they may see | no | no | no | only messages of employees they may see |
| Without these roles | no messaging | no | no | no | no |

A supervisor who addresses a hidden employee gets the same answer as for an employee without contact, and a name matching several employees returns the candidates instead of guessing. Group sends and id-number sends reach only the visible part of the audience. Messages of the owner or of a paired account have no employee behind them and are therefore administrator-only.

`send_message` shows channel, resolved recipient and text and sends only after the confirmation; `read_messages` applies the same scope.

<!-- level:effects -->

## Stage 3 — What the choices cause

- **Linking is the gate for replies.** A reply from an identity that is linked nowhere is dropped, so a person who never pressed START stays silent in Klacks.
- **A channel belongs to exactly one account.** Moving a Telegram chat to a different login account needs the first link removed.
- **A supervisor cannot learn that an employee exists.** A refusal for a hidden employee reads like a missing contact.
- **Owner messages stay out of every supervisor's list**, even when about employees they see.
- **The 15-minute window is deliberate.** A code that travels from screen to chat within seconds is not left open longer; the administrator invitation lasts 72 hours because it travels by e-mail.

### Trigger phrases

- "Wie verknüpfe ich Anna Müller mit Telegram?"
- "Warum kann ich keine Nachricht an mich schicken?"
- "Wie koppele ich mein Konto mit dem Messenger?"
- "Who is allowed to message employees, and who sees the answers?"
- "Why does a message to an employee say no contact?"

<!-- level:pitfalls -->

## Stage 4 — Pitfalls and what to tell the administrator

- **Do not ask anybody to type a Telegram chat id.** It is not known to them; use the invitation or the code.
- **"Me" is the owner, not the asker.** An administrator who writes "send it to me" reaches the owner identity, which may be somebody else's phone.
- **"No contact" does not prove there is none.** For a supervisor it may only mean the employee is outside their groups.
- **An invitation needs a private e-mail.** Fix the master data first when it fails.
- **Codes are for Telegram.** Do not promise a code flow on another channel.
