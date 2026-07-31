---
name: explain_roles_and_permissions
description: |
  Explains the two permission levels a login account can be given — Supervisor and Admin — what each
  one may do, and that an account without either can only look. Covers the boundary that settings,
  user administration and closing a period are administrator-only, that a login account is separate
  from a person's staff record, and what a personal access token is for. Use this when the user asks
  who may do what, why an action is refused, or how to give somebody more rights.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - rechte
  - berechtigung
  - rolle
  - supervisor
  - darf nicht
  - keine berechtigung
  - zugriffstoken
synonyms:
  de: [rechte, berechtigung, rolle, supervisor, administrator, darf das nicht, keine berechtigung, benutzer anlegen, mehr rechte geben, zugriffstoken, benutzerkonto]
  en: [permissions, rights, role, supervisor, administrator, not allowed, no permission, create a user, grant more rights, access token, login account]
  fr: [droits, autorisation, rôle, superviseur, administrateur, non autorisé, créer un utilisateur, jeton d accès]
  it: [diritti, autorizzazione, ruolo, supervisore, amministratore, non autorizzato, creare un utente, token di accesso]
---

# Who may do what

## Core idea (one sentence)

There are exactly two permission levels that can be granted to a login account, and everything else
that sounds like a role is a job description, not a permission.

## The two levels

**Supervisor** may create, edit and delete addresses, groups, contracts, absences and shifts, and
may approve a day or a group in the schedule as well as withdraw that approval again.

**Admin** may do everything a Supervisor may, plus: all settings including user administration
itself, closing and reopening payroll periods, and the specially protected areas such as identity
providers, reports and calendar rules.

An account with **neither** can look but not change. Confirming individual working times is the
exception — that needs no particular level.

Both are granted in user administration and take effect immediately, without a separate save.

## "Planner" is not a level

In daily work it is usually a Supervisor account that does the actual planning — starting automatic
planning, adjusting the grid, submitting days for approval. That is not a third permission level,
just what a Supervisor account can do anyway.

## Where the boundary sits

The dividing line that matters in practice: **settings, user administration and closing a period are
administrator-only.** A Supervisor can approve every day of a month and still not be able to close
it. If an action is refused despite the person "being allowed to plan", this boundary is almost
always the reason.

## Login account and staff record are separate

A login account does not have to be linked to a person's staff record, and a member of staff does not
automatically have a login. User administration and people administration are two different areas —
creating somebody as staff does not give them a way in.

The account currently signed in is shown in user administration but cannot edit itself there, change
its own rights, or delete itself.

## Personal access tokens

A token lets an external program sign in to Klacks on the user's behalf — for connecting other AI
tools, for instance. It is shown **once, in plain text, at creation** and cannot be retrieved
afterwards; it can only be revoked and replaced. A token carries the rights of the account that
created it.

## Related skills

- `list_system_users` / `create_user` / `delete_system_user` — login accounts
- `get_user_permissions` / `assign_user_permissions` — read and grant a level
- `update_my_account` — one's own account
- `create_personal_access_token` / `list_personal_access_tokens` / `revoke_personal_access_token`

## Trigger phrases

- "Why am I not allowed to do that?"
- "Give this person the right to close periods."
- "What is the difference between Supervisor and Admin?"
- "Create a login for the new colleague."
- "Wozu brauche ich ein Zugriffstoken?"
