---
name: explain_klacksy_autonomy
description: |
  Explains the four autonomy levels that decide when Klacksy asks before it writes anything, how an
  action's risk is classified, and how a held action is released by an explicit confirmation. Also
  covers undoing the last change and proactive monitoring. Use this when the user asks why Klacksy
  keeps asking for confirmation, why it acted without asking, how to make it more or less
  independent, or how to take back what it just did.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - autonomie
  - autonomy
  - bestätigung
  - confirmation
  - nachfragen
  - rückgängig
  - undo
  - rollback
  - selbständig
  - freigabe
synonyms:
  de: [autonomie, autonomiestufe, bestätigung, nachfragen, freigabe, selbständig, eigenmächtig, rückgängig machen, zurücknehmen, warum fragst du]
  en: [autonomy, autonomy level, confirmation, ask first, act on its own, undo, roll back, revert, approval]
  fr: [autonomie, niveau autonomie, confirmation, demander avant, annuler, revenir en arrière]
  it: [autonomia, livello autonomia, conferma, chiedere prima, annullare, ripristinare]
---

# Autonomy — when Klacksy asks before acting

## Core idea (one sentence)

Every user decides for their own account how independently Klacksy may act, and Klacksy never
confirms on the user's behalf.

## The four levels

- **Propose** — Klacksy only suggests; every write needs an explicit confirmation.
- **Assisted** — reversible actions and actions inside a test scenario run straight away, everything
  else waits for confirmation.
- **Autonomous** (the default) — everything runs straight away except sensitive actions.
- **Fully autonomous** — multi-step plans also run through without intermediate approvals.

The level is a **personal setting per user account**, not a company-wide switch. Two people using
the same installation can work at different levels.

## How an action is classified

Every action Klacksy can perform carries a risk classification:

- read-only actions always run immediately, at every level;
- reversible or scenario-bound actions need at least *Assisted*;
- irreversible actions need at least *Autonomous*;
- **sensitive actions always require an explicit confirmation, whatever the level** — user
  administration, permission changes, and changing the autonomy setting itself.

So a higher level never means "Klacksy stops asking altogether".

## What happens when confirmation is needed

Klacksy does not run the action. It sets it aside for a short waiting period, summarises what would
happen, and waits. Only when the user agrees in their own words does Klacksy release exactly the
action it set aside — same action, same values. It cannot invent an agreement, and it cannot
release a held action by itself.

## Taking something back

Klacksy can report its own last action and propose the counter-action to undo the most recent
successful change. It does **not** undo anything on its own: it names the counter-action, and the
user decides. Some actions have no automatic counterpart — creating a person, for instance — and
those have to be reversed by hand.

## Acting unprompted

Separately from the levels, Klacksy can watch for situations worth flagging and speak up on its own.
This monitoring can be switched on or off; it changes when Klacksy *starts* a conversation, not what
it is allowed to write once one is running.

## Related skills

- `get_autonomy_level` / `set_autonomy_level` — read and change the personal level
- `confirm_pending_action` — releases a held action after the user agreed
- `verify_my_last_action` — reports what Klacksy did last
- `rollback_my_last_change` — proposes the counter-action for the last change
- `configure_heartbeat` — turns proactive monitoring on or off

## Trigger phrases

- "Why are you asking me again?"
- "Just do it without asking every time."
- "Undo what you just did."
- "How independently can you act?"
- "Warum hast du das ohne Rückfrage gemacht?"
