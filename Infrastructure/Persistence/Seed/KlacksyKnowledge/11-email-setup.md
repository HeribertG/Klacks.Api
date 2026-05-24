---
name: explain_email_setup
description: |
  Step-by-step guide for setting up email (SMTP/IMAP) for a user in Klacks. Use this when the user
  wants to configure email, connect a mailbox, set up sending/receiving mail, or troubleshoot
  SMTP/IMAP connection problems (provider settings, ports, SSL/TLS, authentication, passwords).
category: System
executionType: Skill
alwaysOn: false
triggerKeywords:
  - email
  - e-mail
  - mail
  - smtp
  - imap
  - postfach
  - mailbox
  - gmx
  - gmail
  - outlook
  - posteingang
  - mailserver
synonyms:
  de: [email, e-mail, mail, smtp, imap, postfach, posteingang, mailserver, einrichten, gmx, gmail, outlook, passwort]
  en: [email, mail, smtp, imap, mailbox, inbox, mail server, setup, configure, provider, password]
  fr: [courriel, email, smtp, imap, boîte mail, serveur de messagerie, configurer]
  it: [email, posta, smtp, imap, casella, server di posta, configurare]
---

# Email Setup Wizard (SMTP / IMAP)

When a user wants to set up email, follow this process:

## Step 1: Gather Information
- Ask for the email provider (e.g. GMX, Gmail, Outlook, Yahoo) OR the email address.
- Extract the provider from the email domain (e.g. hans@gmx.ch -> GMX).

## Step 2: Research Provider Settings
- Use web_search to find the correct SMTP and IMAP settings for the provider.
- Search for: "{provider} SMTP server port SSL settings" and "{provider} IMAP server port SSL settings".
- Common settings: server hostname, port, SSL/TLS mode, authentication type.

## Step 3: Configure Settings via UI
- Use update_email_settings for SMTP and update_imap_settings for IMAP — the user sees each field
  being filled in the Settings UI.
- The username is usually the full email address.
- Include all fields found via web search (server, port, SSL, auth type, username).

## Step 4: Password Handling
- If the user provides the password in chat, pass it directly (smtpPassword / password parameter).
- If not, tell them to enter it in Settings > Email > Password and Settings > IMAP > Password.
- Do NOT proactively ask for the password — let the user decide.

## Step 5: Test & Fix (trial and error)
- Use test_smtp_connection, then test_imap_connection.
- On failure, read the error: auth error -> wrong password/auth type (try LOGIN, PLAIN);
  SSL error -> try other SSL mode or port (587 STARTTLS, 465 SSL); connection refused -> wrong
  server/port; timeout -> check server name, toggle SSL. Max 3 retries per issue.

## Step 6: Confirm Success
- Report the final working configuration (server, port, SSL, auth type for SMTP and IMAP).

## Important Rules
- Always web_search first, don't guess server settings (fall back to known providers if web search is off).
- Always test after configuration. Be transparent about each step — the user sees all changes live.
