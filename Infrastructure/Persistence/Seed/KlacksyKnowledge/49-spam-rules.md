---
name: explain_spam_rules
description: |
  Explains filtering unwanted incoming mail: four kinds of rule, where each one looks — inside the
  sender address or display name, at the sender's domain, in the subject line, or in the message
  text — and that the entered text is always compared literally rather than as a search expression.
  Covers that a match moves the message to the junk folder on the server rather than deleting it,
  that filtering happens while fetching, and that a changed rule re-sorts mail that already arrived.
  Use this when the user asks how to stop a sender or why unwanted mail still arrives.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - spam
  de:
    - spam-regel
    - absender blockieren
    - junk ordner
    - unerwünschte mail
  en:
    - spam rule
    - block sender
    - junk folder
synonyms:
  de: [spam-regel, spamregel, absender blockieren, domain blockieren, junk ordner, unerwünschte mails, werbemails filtern, betreff filtern, warum kommt der spam trotzdem]
  en: [spam rule, block sender, block domain, junk folder, unwanted mail, filter subject, why does spam still arrive]
  fr: [règle anti-spam, bloquer un expéditeur, bloquer un domaine, dossier indésirable, courrier indésirable]
  it: [regola antispam, bloccare mittente, bloccare dominio, cartella posta indesiderata]
---

# Spam rules — keeping unwanted mail out of the inbox

## Core idea (one sentence)

A short list of rules; a message matching any of them is moved to the junk folder while it is being
fetched.

## The four kinds of rule

Card anchor: `spam-rules-container`. Each rule has a kind, a pattern and an active switch.

| Kind | Where the pattern is looked for | How it is compared |
|---|---|---|
| **Sender contains** (de: "Absender enthält") | the sender's address **and** their display name | the pattern must appear somewhere inside |
| **Sender domain** (de: "Absender-Domain") | the part after the `@` of the sender's address | must match **exactly** |
| **Subject contains** (de: "Betreff enthält") | the subject line | must appear somewhere inside |
| **Body contains** (de: "Inhalt enthält") | the message text **and** its formatted version | must appear somewhere inside |

Capitalisation never matters.

**Three things that surprise people:**

- **Patterns are plain text, never search expressions.** There is no rule kind that interprets
  wildcards or a search syntax. Something like `.*@example\.com` is looked for letter by letter and
  will not match anything sensible.
- **A sender domain does not cover its sub-domains.** The comparison is exact, so `example.com`
  does **not** catch an address ending in `mail.example.com`. Such a sender needs its own rule, or
  a "sender contains" rule instead.
- **Searching the message text also searches its formatting.** A word hidden in the markup of a
  formatted message can trigger a match that is invisible in the readable text.

## What happens on a match

The message is **moved to the junk folder**, both in Klacks and on the mail server itself. It is
**not deleted**, and it carries no spam marking of its own — the folder is the whole record. A
message classified as junk is not analysed further and is not linked to anybody.

**There is no allow list.** All four kinds block; there is no rule kind that protects a sender, so
there is no question of which one wins. Individual messages can be marked as junk or not junk by
hand from the inbox, but that affects only that message and creates no rule.

Rules are evaluated in their given order and the first match ends the evaluation — but since every
match has the same consequence, the order only affects which rule is named as the reason.
Deactivated rules are skipped.

## When filtering happens

**While mail is fetched**, not while it is displayed. Mail is collected in the background at a
configurable interval, five minutes by default.

**A changed rule also applies to mail that already arrived.** Creating, changing or deleting a rule
triggers a re-sort: messages that now match move to junk, and messages in junk that no longer match
**move back**. So a rule that was too broad can be corrected without losing anything.

## Related skills

- `list_spam_rules`, `create_spam_rule`, `update_spam_rule`, `delete_spam_rule`

## Trigger phrases

- "Wie blockiere ich diesen Absender?"
- "Can I use a wildcard in a spam rule?"
- "Ich habe die Domain eingetragen, es kommt trotzdem durch."
- "Werden die Mails gelöscht?"
- "Gilt die Regel auch für Mails, die schon da sind?"
