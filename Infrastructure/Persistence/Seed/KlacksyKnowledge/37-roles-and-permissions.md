---
name: explain_roles_and_permissions
description: |
  Explains the two permission levels a login account can be given — Supervisor and Admin — what each
  one may do, and that an account without either is a planner: it reads and plans. Covers the boundary that settings,
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

## Roles are work areas

Each level stands for a work area, and every right follows from it:

- **Planner** (no role): creates and maintains the plan — placing, changing and removing entries,
  autofill, recording absences.
- **Supervisor**: orders and shifts (create, edit, seal, cut and delete them, containers included),
  creating and maintaining employees (their contract assignments and group memberships included), plus
  all planner work.
- **Admin**: all settings — among them the contract templates and the group structure (creating,
  editing, moving and deleting groups), the scheduling rules and user administration — plus deleting
  employees and other master data, plus everything a Supervisor and a planner may do.

So assigning a contract to a person or putting a person into a group is employee maintenance
(Supervisor), while creating or changing the contract template or the group itself is a setting (Admin).

## The two levels

**Supervisor** may create and edit employees and addresses — assigning their contracts and putting
them into groups or taking them out included — and may create, edit and delete orders and shifts
(a sealed order only while nothing is planned on it), but may not delete employees. Contract
templates, the group structure and the scheduling rules are settings and stay with Admin. A
Supervisor may approve a day or a group in the schedule as well as withdraw that approval again.

**Admin** may do everything a Supervisor may, plus: all settings including user administration
itself, the contract templates, the group structure and the scheduling rules, deleting employees,
groups and contracts, closing and reopening payroll periods, and the specially protected areas such
as identity providers, reports and calendar rules.

An account with **neither** is a planner: it can read everything it is allowed to see and can still
plan — place, change and remove entries in the schedule, let the automatic planning (autofill) fill a
group it sees and accept or reject the resulting scenario, record absences, record, change and delete
expenses, plan notes and planning commands (the day keywords such as FREE or EARLY), write client
notes and work with Klacksy. It cannot create or edit master data (employees, orders and
shifts, contract templates, groups) and has no settings. Confirming individual working times needs no particular level either.

Both are granted in user administration and take effect immediately, without a separate save.

## The planner is the level without a role

There is no separate "planner" role to grant: an account without Supervisor or Admin is the planner
level described above — it reads and plans. Supervisor adds orders, shifts, employees and
approvals on top; Admin adds the settings.

## Only Supervisor and Admin

The following belong to Supervisor and Admin only, never to a planner account: changing Klacksy's
autonomy level, recurring tasks, starting or approving multi-step plans, and creating
personal access tokens in the token settings or through Klacksy. A planner may still look at the
multi-step plans of their own account and abort one that is still running.
The inbox (received e-mails) is also for Supervisor and Admin only; deleting e-mails for good, mail
folders and spam rules are for the Admin alone. Proactive governance is Admin-only, and the messaging
page needs Supervisor or Admin and an active messaging add-on.

## Placeholders

Creating placeholders (rows for people who are not in the system yet) and converting one into an employee
or an external employee are Supervisor and Admin work, like creating and editing any staff record. A
planner plans on the placeholders that exist; deleting one is Admin-only (`explain_placeholder_employees`).

## Where the boundary sits

The dividing line that matters in practice: **settings, user administration and closing a period are
administrator-only.** A Supervisor can approve every day of a month and still not be able to close
it. If an action is refused despite the person "being allowed to plan", this boundary is almost
always the reason. Example: a planner may recalculate the hours of open periods, but recalculating a frozen (closed
and exported) period is administrator-only and needs a reason.

## The assistant's own configuration

Creating or changing an assistant agent (its name, display name and description) is **Admin-only**. Everybody
may use the assistant; only an administrator may change how it is configured.

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
