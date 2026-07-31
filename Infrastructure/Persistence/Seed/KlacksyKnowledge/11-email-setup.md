---
name: explain_email_setup
description: |
  Explains how Klacks connects to a mailbox: one setting block for sending and a separate one for
  receiving, each with server, port, encryption and sign-in name, plus a connection test for each
  side. Covers why sending can work while receiving fails, what the usual encryption and port
  combinations are, and how Klacksy helps set it up. Use this when the user wants to connect a
  mailbox, asks why mail is not arriving or not going out, or hits an authentication or certificate
  error.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - smtp
  - imap
  - postfach
  - mailserver
  - mail einrichten
  - port
synonyms:
  de: [smtp, imap, postfach einrichten, mailserver, mail geht nicht raus, mail kommt nicht an, port, verschlüsselung, anmeldung fehlgeschlagen, zertifikatsfehler]
  en: [smtp, imap, connect mailbox, mail server, mail not sending, mail not arriving, port, encryption, authentication failed, certificate error]
  fr: [smtp, imap, configurer la boîte mail, serveur de messagerie, le courrier ne part pas, port, chiffrement]
  it: [smtp, imap, configurare la casella, server di posta, la posta non parte, porta, crittografia]
---

# Connecting a mailbox — sending and receiving are two things

## Core idea (one sentence)

Klacks talks to a mailbox over two separate connections — one for sending, one for receiving — and
each is configured and tested on its own.

## Why the split matters

Sending and receiving use different protocols, different servers and often different ports. So the
most common confusion has a simple explanation: **outgoing mail works while incoming stays empty**,
or the other way round. One side is configured correctly, the other is not. Each side has its own
connection test, and both have to pass.

## What each side needs

- **Server address** of the provider.
- **Port** — which one depends on the encryption method.
- **Encryption** — either a connection encrypted from the start, or one upgraded after connecting.
- **Sign-in name**, which for most providers is the full email address.
- **Password**, stored separately from the rest of the settings.

The usual pairings for sending are port 587 with upgrade-after-connect, or port 465 encrypted from
the start. Providers publish their own values, and they differ.

## How Klacksy helps

Asked to set up mail, Klacksy works through it in order: it derives the provider from the email
address, looks up that provider's current settings rather than guessing them, enters them field by
field so the changes are visible on screen, and then runs both connection tests.

If a test fails, the error says which side to fix:

- **Authentication rejected** — wrong password or sign-in name. Many providers require an
  app-specific password rather than the normal account one.
- **Encryption error** — wrong combination of port and method; the other pairing usually works.
- **Connection refused** — wrong server or wrong port.
- **Timeout** — server name wrong, or the encryption setting does not match what the server expects.

Klacksy will not ask for the password on its own. It can be given in the conversation, or entered
directly in the settings.

## Related skills

- `get_email_settings` / `update_email_settings` — the sending side
- `get_imap_settings` / `update_imap_settings` — the receiving side
- `test_smtp_connection` / `test_imap_connection` — check each side
- `fetch_new_emails` — retrieve mail once receiving works

## Trigger phrases

- "Set up my mailbox."
- "Why is no mail arriving?"
- "Sending works but receiving does not."
- "Which port do I need?"
- "Die Anmeldung am Mailserver schlägt fehl."
