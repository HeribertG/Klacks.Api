---
name: explain_messenger_channels
description: |
  Explains which of the twelve supported messengers can both send and receive (Telegram, WhatsApp, Slack,
  LINE, Viber) and which can only send (Signal, SMS, Threema, KakaoTalk, WeChat, Zalo, Microsoft Teams),
  why a reply or a choice question never arrives over a send-only channel, that messages from unknown
  senders are dropped, how long the history is kept and that messaging is an optional module.
  Use when somebody asks which messenger can answer, which one only notifies or how long messages stay.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - welche messenger empfangen
    - messenger nur senden
    - antworten per messenger empfangen
    - wie lange bleiben messenger-nachrichten
    - welche messenger kann klacks
  en:
    - which messengers receive
    - messenger send only
    - receive replies by messenger
    - how long are messenger messages kept
    - which messengers can klacks use
synonyms:
  de: [welcher messenger kann antworten, messenger die nur benachrichtigen, kann ich per signal eine antwort bekommen, wie lange werden nachrichten aufbewahrt, ist messaging optional]
  en: [which messenger can reply, messengers that only notify, can I get an answer over signal, how long are messages stored, is messaging optional]
  fr: [quelle messagerie peut répondre, messageries qui notifient seulement, combien de temps les messages sont conservés]
  it: [quale messenger può rispondere, messenger che solo notificano, per quanto tempo si conservano i messaggi]
---

# Messenger channels — which can answer, which only notify, and how long messages stay

## Core idea (one sentence)

Messaging is an **optional module**; of its twelve channels **five carry a conversation in both directions** and **seven only deliver outgoing messages**, and an incoming message is kept only when its sender is known to Klacks.

<!-- level:short -->

## Stage 1 — What it is and what it is for

Klacks can send messages to employees, to the owner and to login accounts through external messengers, and can store what comes back. Messaging is not part of the core: it is a feature plugin an administrator installs and switches on per installation. Without it there are no messaging skills and no messaging page, so never promise a message before the module is on (the plugin list shows whether it is installed and enabled).

| Channel | Receives replies | Sends | Recipient is addressed by |
|---|---|---|---|
| Telegram | yes | yes | chat id |
| WhatsApp | yes | yes | phone number |
| Slack | yes | yes | Slack user id |
| LINE | yes | yes | LINE user id |
| Viber | yes | yes | Viber user id |
| Signal | no | yes | phone number |
| SMS | no | yes | phone number |
| Threema | no | yes | Threema id |
| KakaoTalk | no | yes | KakaoTalk id |
| WeChat | no | yes | WeChat id |
| Zalo | no | yes | Zalo id |
| Microsoft Teams | no | yes (posts a card into a channel) | channel address |

"Receives" is a property of the channel itself: the seven send-only channels have no way to hand a reply back, whatever is configured.

<!-- level:elements -->

## Stage 2 — What has to be true before a reply arrives

1. **The channel is switched on.** A disabled channel accepts nothing and is not polled.
2. **Klacks must be reachable by the messenger.** Replies arrive through a callback address (a webhook) that the messenger calls on a publicly reachable URL, and the call is verified (Telegram refuses every call when no secret is stored for the channel). An installation on a workstation or behind a router without a public address therefore receives nothing - with one exception: **Slack can also be polled** every 10 seconds, but only when a Slack channel **id** is configured (a `#name` does not count). The first polling round starts at "now", so the older channel history is not replayed.
3. **Only text arrives.** Pictures, voice messages, stickers and bot messages are ignored by the five receiving channels.
4. **The sender must be known.** A reply is stored only when the sender id matches an employee's messenger contact of that channel, an entry of the owner, or a paired login account. A message from anybody else is **dropped, not stored**, and only noted in the log once per hour and sender. A reply is then missing in the message list without any error.
5. **Retention.** An hourly job keeps the **newest N messages per channel** (both directions together) and deletes the older ones for good. N is one installation-wide value (default 1000) applied to each channel separately, disabled channels included. There is no time limit: a busy channel loses its history within days, a quiet one keeps it for months.

Reading and sending themselves are done with `read_messages` and `send_message`; whether a given channel is wired up correctly is the job of `diagnose_messaging_setup`. `list_feature_plugins` shows whether the messaging module is installed and enabled.

<!-- level:effects -->

## Stage 3 — What the channel choice causes

- **A send-only channel can only notify, never ask.** A message with fixed answer choices (a numbered reply) is refused on the seven send-only channels, because the answer could never come back. It is offered only on Telegram, WhatsApp, Slack, LINE and Viber. No channel renders native buttons yet; the choices are a numbered list the recipient answers by typing the number.
- **A phone number is accepted as recipient only on WhatsApp, Signal and SMS.** On every other channel the person needs a stored messenger identity first.
- **Deleting by count means an unread answer can vanish.** On a busy channel an answer nobody opened can be pushed out by newer messages.
- **A sender unknown today stays invisible.** Messages dropped earlier are not recovered when the sender is linked later.

### Trigger phrases

- "Welche Messenger kann ich zum Antworten nutzen?"
- "Kann ich über Signal eine Rückmeldung bekommen?"
- "Wie lange werden Messenger-Nachrichten aufbewahrt?"
- "Which of the messengers only sends and never receives?"
- "Is messaging part of every Klacks installation?"

<!-- level:pitfalls -->

## Stage 4 — Pitfalls and what to tell the administrator

- **"Receives" does not mean "works".** A receiving channel can still stay silent because of a missing public address, a missing secret or an unknown sender; do not blame the channel list.
- **Do not suggest a send-only channel when an answer is needed.** For approvals or questions use one of the five receiving channels.
- **Messaging may not be installed at all.** Say so instead of describing features the installation does not have.
- **Retention is not an archive.** Anything that must be kept longer than the newest N messages has to be saved elsewhere.
