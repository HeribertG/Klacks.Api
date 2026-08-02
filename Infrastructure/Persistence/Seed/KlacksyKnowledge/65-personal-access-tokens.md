---
name: explain_personal_access_tokens
description: |
  Explains what a personal access token is for and how it differs from signing in directly: it
  carries exactly the same rights as the account that created it, with no reduced scope. One
  exception is deliberate — a token can never be used to mint a further token for itself, only an
  actual sign-in can do that, so a leaked token cannot multiply itself. Also covers the default and
  allowed length of its validity. Use this when asked what a token can do or how long it lasts.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - token
  de:
    - persönliches token
    - token gültigkeit
  en:
    - personal token
    - token validity
synonyms:
  de: [token hat dieselben rechte wie login, mit token weitere token erstellen, wie lange ist ein zugriffstoken gültig, gültigkeitsdauer eines zugriffstokens, maximale laufzeit eines tokens]
  en: [token carries the same rights as login, create a new token using a token itself, how long is a personal access token valid, validity period of an access token, maximum lifetime of a token]
  fr: [le jeton a les mêmes droits que la connexion, durée de validité maximale d'un jeton]
  it: [il token ha gli stessi diritti del login, durata massima di validità di un token]
---

# Personal access tokens — same rights, one exception

## Core idea (one sentence)

A personal access token acts exactly like the person who created it whenever it is used, with a
single deliberate carve-out: it cannot be used to create a further token for itself.

## Full rights, no reduced scope

When a request arrives carrying a token instead of a normal sign-in, the system rebuilds the same
set of rights the token's owner would have from a fresh login — same roles, same permissions. A
token is not a limited, read-only or partial stand-in; anything the owner's account may do, the
token may do too, for as long as it is valid.

## The one thing a token cannot do

Creating a new personal access token always requires an actual sign-in — presenting a token to mint
another one is refused. This is a deliberate choice: if a token could create further tokens, a
single leaked one could multiply itself indefinitely, and revoking the original would no longer
contain the damage. Requiring a real sign-in for this one step puts a hard ceiling on what a stolen
token can do.

## How long a token lasts

A newly created token is valid for 365 days unless a different length is chosen, and the chosen
length must fall between 1 and 730 days. There is no option for a token that never expires.

## Related skills

- `create_personal_access_token` — issue a new token, choosing its validity within the allowed range
  (always requires an actual sign-in)
- `list_personal_access_tokens` / `revoke_personal_access_token` — manage existing tokens

## Trigger phrases

- "Kann ich mit einem Token einen weiteren Token erstellen?"
- "Wie lange ist mein Zugriffstoken gültig?"
- "Does a personal access token have the same rights as logging in?"
- "Can I use a token to create another token?"
