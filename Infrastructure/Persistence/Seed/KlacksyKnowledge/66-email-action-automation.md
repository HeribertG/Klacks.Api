---
name: explain_email_action_automation
description: |
  Explains how an incoming employee email can turn into a planning action on its own: which
  intents are recognized, why customer email is never acted on, and the safeguards - autonomy,
  detection confidence, contract type, an already-planned period, an already-sealed period -
  deciding whether Klacksy executes the action or only suggests it. Use this when a sickness
  cover, vacation entry or planning command appeared by itself, or a clear email produced
  nothing.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - email
  de:
    - automatische planungsaktion
    - e-mail löst aktion aus
    - krankmeldung per mail
  en:
    - automatic planning action
    - email triggers action
    - sick leave from email
synonyms:
  de: [warum wurde automatisch eine krankmeldung angelegt, warum hat klacksy von selbst gehandelt, e-mail hat automatisch etwas geplant, warum wurde nichts aus der mail gemacht, wann handelt klacksy bei mails automatisch, ferienwunsch per mail wurde nicht übernommen]
  en: [why did a sick-leave scenario appear automatically, why did klacksy act on this email by itself, email triggered a planning action, why was nothing done despite a clear email, when does klacksy act automatically on emails, vacation request from email was not applied]
  fr: [pourquoi un scénario de maladie est apparu automatiquement, pourquoi klacksy a agi tout seul sur cet e-mail, l'e-mail a déclenché une action de planification, demande de vacances par e-mail non appliquée]
  it: [perché è apparso automaticamente uno scenario di malattia, perché klacksy ha agito da solo su questa email, l'email ha attivato un'azione di pianificazione, richiesta di ferie via email non applicata]
---

# Email action automation — when a mail becomes a planning action by itself

## Core idea (one sentence)

An incoming email from a known employee is read for its intent, and — only if several independent
safeguards all agree — Klacksy carries out the matching planning action without being asked.

## Before anything is read

A company-wide switch must be on, the sender must resolve to a recorded employee/contact
(unrecognized senders are never analyzed), and the mail must not already be junk.

## What gets recognized

Five intents: **work cancellation** (sick/unable to attend an already-scheduled shift), **vacation
request**, **day-off wish** (unavailable future days without an existing shift), **availability
announcement** (a clock-time window), **shift preference** (can/cannot work mornings/evenings/
nights). The same reading rates its own confidence as high or low. **A customer email is always
just summarized** — it never triggers a planning action, regardless of anything else.

## The safeguards — not the same for every intent

This flow's autonomy mapping is stricter than everyday chat actions; the **effective level is the
minimum across every admin account** — none set up means suggestion-only.

| Intent | Min. autonomy | High confidence | Zero-hour contract | Blocked by existing shifts | Blocked by sealed period |
|---|---|---|---|---|---|
| Work cancellation (sick) | Autonomous | yes | no | no | **yes** |
| Vacation request | Fully autonomous | yes | no | **yes** | no |
| Day-off wish | Fully autonomous | yes | **yes** | **yes** | no |
| Availability announcement | Fully autonomous | yes | **yes** | **yes** | no |
| Shift preference | Fully autonomous | yes | **yes** | **yes** | no |

A sickness report is expected in an already-planned period, so it is only refused once that period
is sealed. The other four are future wishes: they execute only into unplanned periods, and only for
contracts without guaranteed hours. Everywhere: no identifiable date, a self-contradicting wish
("only nights" and "no nights" together), or a very long span (roughly three months+) falls back to
a suggestion.

**Even an executed sick-cover is not a done deal** — it creates a proposed cover scenario needing a
separate acceptance before it touches the real schedule, and only runs when the employee sits in
exactly one group with exactly one matching absence type (by name, e.g. "krank"/"sick").

## What always happens regardless

Every planner and admin gets a summary of the email either way — live in chat if connected,
otherwise waiting for their next turn. Nothing executed is never nothing said.

## Related skills

`get_email_analysis`, `get_autonomy_level`/`set_autonomy_level`

## Trigger phrases

- "Warum wurde automatisch eine Krankmeldung angelegt?"
- "Why did Klacksy act on this email without asking?"
- "Does a customer email ever trigger a planning action?"
