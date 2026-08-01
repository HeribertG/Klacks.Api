---
name: explain_identity_providers
description: |
  Explains signing in with an existing company directory or an external sign-in service: connecting
  a directory server by host, port, base entry and search filter, or a redirect-based service by
  client identifier, secret and the three endpoint addresses. Covers that this is added alongside
  the ordinary password login and never replaces it, that an account is created on first successful
  sign-in, and that such an account arrives without any permissions. Use this when the user asks
  about single sign-on, connecting a company directory, or why a newly arrived person sees nothing.
category: Query
executionType: Skill
alwaysOn: false
parameters:
  - name: level
    type: enum
    required: false
    enumValues: [short, elements, effects]
triggerKeywords:
  mul:
    - ldap
    - oauth
    - single sign-on
  de:
    - anmeldedienst
    - verzeichnisdienst
    - firmenverzeichnis
    - anmeldung über
  en:
    - identity provider
    - directory server
    - sign in with
synonyms:
  de: [identity provider, anmeldedienst, verzeichnisdienst, firmenverzeichnis, single sign-on, anmeldung mit firmenkonto, active directory anbinden, warum hat der neue benutzer keine rechte]
  en: [identity provider, directory server, single sign-on, sign in with company account, connect active directory, new user has no permissions]
  fr: [fournisseur d identité, annuaire d entreprise, authentification unique, connexion avec compte d entreprise]
  it: [provider di identità, directory aziendale, single sign-on, accesso con account aziendale]
---

# Signing in through an existing directory or service

<!-- level:short -->

## Stage 1 — What this is for

Instead of maintaining a separate password in Klacks, people can sign in with the account they
already have. Four kinds of connection are supported
(de: "Identity Provider", fr: "Fournisseur d'identité", it: "Provider di identità"):

- **Directory server** and **company directory** — the classic on-premise directories. People type
  their usual name and password in the normal sign-in form.
- **OAuth 2.0** and **OpenID Connect** — redirect-based services. People click a button, authorise
  at the provider, and come back signed in.

**This is always an addition, never a replacement.** The ordinary password login stays available; a
connection cannot switch it off.

Each connection has two independent purposes, and either can be used on its own:

- **Use for authentication** — people may sign in through it.
- **Use for staff import** — staff records are read from it. This creates **staff entries**, not
  sign-in accounts.

<!-- level:elements -->

## Stage 2 — The fields

Card anchor: `identity-providers-card`.

**Always visible** — name, kind, enabled, sort order, and the two purpose switches above. Sort
order matters: directory connections are tried in that order, and the first success wins.

**Directory server / company directory** (tab de: "Verbindung", en: "Connection")

Host, port, whether the connection is encrypted, the base entry to search below, the account used
to search, its password, and the filter that selects people. Defaults are filled in: port 389, or
636 once encryption is switched on, and a common filter for person entries.

The two kinds differ in more than the label. They use different sign-in procedures and build the
user's entry differently — one by common name, the other by user identifier. The company-directory
kind additionally retries with the simpler procedure when the first attempt fails.

**OAuth 2.0 / OpenID Connect** (tab "OAuth")

Client identifier, client secret, the address people are sent to, the address where the token is
fetched, the address where the person's details are read, the requested scopes, and a tenant
identifier where the service needs one.

Both kinds read the person's details from the details endpoint. The difference between them is a
sign-out address that is derived for the second kind only.

**Testing** — the dialog can test the connection and report how many people were found, with a few
examples, before anything is saved.

**No field is enforced by the server.** All connection fields are optional in the data model; a
connection can be saved incomplete and will then simply fail at sign-in. The test button is the way
to find that out.

<!-- level:effects -->

## Stage 3 — What happens on first sign-in

**An account is created automatically, without anyone approving it.**

For directory sign-in, the local password check runs first; only when it fails is the directory
consulted. Existing people are matched by e-mail address. When the directory name is not an e-mail
address, one is synthesised from it so a match key exists.

For redirect-based sign-in, the match is by e-mail address only. A service that returns no e-mail
address cannot sign anybody in.

**Such an account arrives with no permissions at all.** There is no mapping from directory groups,
claims or scopes onto roles in Klacks. Somebody has to grant the rights afterwards. If a newly
arrived person reports an empty screen, this is why.

Stored secrets — the directory search password and the client secret — are encrypted, and both are
returned masked once saved. Re-saving with the mask in place keeps the stored value.

## Related skills

- `list_identity_providers`, `create_identity_provider`, `update_identity_provider`, `delete_identity_provider`
- `test_identity_provider_connection`

## Trigger phrases

- "Can people sign in with their Windows account?"
- "Wie binde ich unser Firmenverzeichnis an?"
- "Der neue Mitarbeiter kann sich anmelden, sieht aber nichts."
- "Muss ich die Passwörter dann noch in Klacks pflegen?"
- "Werden die Gruppen aus dem Verzeichnis übernommen?"
