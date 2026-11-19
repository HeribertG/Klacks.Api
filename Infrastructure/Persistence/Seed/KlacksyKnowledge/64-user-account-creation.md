---
name: explain_user_account_creation
description: |
  Explains that a login account can come from different sources with different outcomes: one
  immediately emails a time-limited password-reset link, one only creates the account and sends no
  mail, and a third (a person ticked in the approval list of a company directory) creates an account
  with no password and no mail at all because the person signs in with the directory password. Also covers how someone who forgot their password gets back in on their own,
  through the same kind of reset link requested from the login page, and how the very first
  administrator of a new installation is created on the first visit, before anyone signs in. Use this
  when a new colleague never received a welcome mail, nobody knows how their first password gets set,
  or someone asks why a fresh installation opens with "create your own admin account", or why a
  person from the company directory has no "send password" link.
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
  de: [neuer benutzer bekommt keine mail, konto ohne willkommensmail von klacksy, wie bekommt jemand sein erstes passwort, assistent legt konto an ohne mailversand, konto aus verzeichnis ohne passwort, kein passwort senden für verzeichnisbenutzer, eigenes passwort selbst zurücksetzen, eigenes admin-konto einrichten, erster start nach der installation]
  en: [new user never got a password email, account created without a welcome mail, how does someone get their first password, assistant creates account without sending mail, directory account has no password, no password link for directory user, reset your own forgotten password, create your own admin account, first start after installation]
  fr: [nouveau compte sans e-mail de bienvenue, comment obtenir son premier mot de passe, réinitialiser soi-même son mot de passe oublié]
  it: [nuovo account senza email di benvenuto, come ottenere la prima password, reimpostare da soli la password dimenticata]
---

# How a login account gets its first password

## Core idea (one sentence)

A login account can be created through two ordinary routes, and only one of them sends the new
person an email telling them how to get in. A third route, approving a person from a company
directory, creates an account that never needs a password from Klacks at all.

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

## Third route: approved from a company directory

When an administrator ticks a person in the approval list of a directory connection (see
`explain_identity_providers`), Klacks creates the account without sending any mail and without any
password: the person signs in with the password of the directory. There is **no reset link** and nothing
to receive, and the account has no role until one is granted in user administration. User
administration marks such an account as (de: "Nur Verzeichnis-Anmeldung", en: "Directory sign-in only", fr: "Connexion par annuaire uniquement", it: "Solo accesso tramite directory")
and shows no password link (de: "Passwort zurücksetzen", en: "Send Access Credentials", fr: "Envoyer les informations d'accès", it: "Invia Credenziali di Accesso") for it; a password reset requested from the login page does nothing for it either.

An existing Klacks account is never linked to the directory by its e-mail address. If the directory
person's e-mail address is already used by a Klacks account, the tick is refused; delete that account or
change its e-mail address first, then tick again, and a new account without password and role is created.

## Getting back in with a forgotten password

Anyone with an account can request a password reset themselves at any time from the login page,
without needing an administrator. It produces the same kind of 24-hour link as the automatic
welcome mail — following it lets the person choose a new password directly.

## The first administrator of a new installation

A new installation ships with a built-in test administrator whose password is public. On the very first
visit Klacks therefore opens the page "Create your own admin account" (de: "Eigenes Admin-Konto
einrichten") directly, before any sign-in: first and last name, email, password (typed twice, the
password rules are listed live). The email is the sign-in name afterwards. Saving creates the new
administrator, switches the built-in test administrator off for good and signs the new administrator in.
Next comes the page "Country and industry" (de: "Land und Branche"), see `explain_active_industries`.

- This works only once and only while nobody has used the built-in account yet: when its password was
  changed, it was already used to sign in, or another administrator exists, the normal sign-in page
  appears instead. Signing in with the built-in account then still leads to the same setup page.
- On a server installation (Docker) the page also asks for the **setup code** (de: "Einrichtungscode"), even in a
  browser on the server itself: the installer printed it at the end, and it stays in the file `.env` as
  `KLACKS_SETUP_CODE`. A wrong code gives "The setup code does not match". While such a code is set, the built-in
  test account cannot be used to sign in from a browser. Without a configured code (an older installation, or a
  Docker Compose setup with an empty value) the sign-in page appears instead. On the office PC installation
  (Windows, opened in a browser on that same PC) no code is needed; opened from another computer, only the
  sign-in page appears.
- If two people submit at the same moment, only the first one becomes administrator; the second gets
  "already completed" and goes to the sign-in page.
- The public test environment and developer installations keep the built-in account and never show the page.

## Related skills

- `create_user` — creates an account through the assistant (no mail sent)
- `explain_identity_providers` — approving people from a company directory

## Trigger phrases

- "Der neue Benutzer hat keine E-Mail bekommen — was jetzt?"
- "Wie bekommt ein neu angelegtes Benutzerkonto sein erstes Passwort?"
- "I created a user with the assistant, why didn't they get an email?"
- "Warum gibt es für den Verzeichnis-Benutzer keinen Link 'Passwort senden'?"
- "Why has the person from our company directory no password in Klacks?"
- "How does someone reset a forgotten password themselves?"
- "Warum zeigt Klacks nach der Installation zuerst 'Eigenes Admin-Konto einrichten'?"
- "I installed Klacks, how do I get the first administrator account?"
- "Wo finde ich den Einrichtungscode?"
