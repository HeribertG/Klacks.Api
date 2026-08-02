---
name: explain_user_account_creation
description: |
  Explains that a login account can come from two different sources with different outcomes: one
  immediately emails a time-limited password-reset link, the other only creates the account and
  sends no mail. Also covers how someone who forgot their password gets back in on their own,
  through the same kind of reset link requested from the login page. Use this when a new colleague
  never received a welcome mail or nobody knows how their first password gets set.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - password
  de:
    - woher kommt das passwort
    - passwort erhalten
  en:
    - where does the password come from
    - receive password
synonyms:
  de: [neuer benutzer bekommt keine mail, konto ohne willkommensmail von klacksy, wie bekommt jemand sein erstes passwort, assistent legt konto an ohne mailversand, eigenes passwort selbst zurücksetzen]
  en: [new user never got a password email, account created without a welcome mail, how does someone get their first password, assistant creates account without sending mail, reset your own forgotten password]
  fr: [nouveau compte sans e-mail de bienvenue, comment obtenir son premier mot de passe, réinitialiser soi-même son mot de passe oublié]
  it: [nuovo account senza email di benvenuto, come ottenere la prima password, reimpostare da soli la password dimenticata]
---

# How a login account gets its first password

## Core idea (one sentence)

A login account can be created through two different routes, and only one of them sends the new
person an email telling them how to get in.

## Two routes, two outcomes

| Created in user administration (Settings) | Created through the conversational assistant |
|---|---|
| A password-reset email is sent automatically | No email is sent at all |
| The reset link is valid for 24 hours | — nothing to receive, nothing expires |
| The person clicks the link and sets their own password | The person must be told separately to use "forgot password" on the login page |

Both routes give the account a real, working password behind the scenes, generated automatically —
nobody types or sees it, and it is not meant to ever be used. That is deliberate: the whole point of
the account is that its actual owner sets a password of their own on first use. The two routes differ
only in whether that step is kicked off automatically by mail or has to be started by hand.

## The nuance that matters

When an account is created through the assistant, the confirmation says the new person should set
their own password using the reset link on the login page — and that advice is correct, it works.
What it does not say outright is that this only works because the person requests it themselves;
nothing was sent to them automatically. Whoever created the account this way needs to pass on that
one extra step by hand, typically by telling the new colleague to go to the login page and use
"forgot password?" with their email address.

## Getting back in with a forgotten password

Anyone with an account can request a password reset themselves at any time from the login page,
without needing an administrator. It produces the same kind of 24-hour link as the automatic
welcome mail — following it lets the person choose a new password directly.

## Related skills

- `create_user` — creates an account through the assistant (no mail sent)

## Trigger phrases

- "Der neue Benutzer hat keine E-Mail bekommen — was jetzt?"
- "Wie bekommt ein neu angelegtes Benutzerkonto sein erstes Passwort?"
- "I created a user with the assistant, why didn't they get an email?"
- "How does someone reset a forgotten password themselves?"
