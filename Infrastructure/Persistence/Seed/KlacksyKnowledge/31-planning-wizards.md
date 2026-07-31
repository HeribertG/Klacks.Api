---
name: explain_planning_wizards
description: |
  Explains the machinery behind automatic planning: three stages that run one after another. Stage
  one fills the plan with a genetic algorithm and no language model, stage two smooths the result
  without touching coverage, stage three polishes it with a picture-reading language model and is
  the only one that costs anything. Covers what each stage does, when to run one alone, how long a
  run takes and how to stop it. Use this when the user asks how auto-planning works internally,
  which stage to use, why a run is slow or costly, or how to cancel one.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - wizard
  - assistent
  - autofill
  - automatisch planen
  - genetischer algorithmus
  - harmonizer
synonyms:
  de: [wizard, assistent, autofill, automatisch füllen, automatische planung, genetischer algorithmus, harmonizer, plan abbrechen, wie lange dauert die planung]
  en: [wizard, autofill, automatic planning, genetic algorithm, harmonizer, cancel the run, how long does planning take]
  fr: [assistant, remplissage automatique, planification automatique, algorithme génétique, annuler la planification]
  it: [assistente, riempimento automatico, pianificazione automatica, algoritmo genetico, annullare la pianificazione]
---

# The planning assistants — three stages, one chain

## Core idea (one sentence)

Automatic planning is not one step but three, and only the last one uses a language model.

## The three stages

**Stage one — filling.** Builds the initial plan and decides who works which shift. It runs a
**genetic algorithm**: thousands of plan variants are generated, scored and recombined until a
balanced one stands. Hard rules always win — working-time law, rest periods, qualifications and
availability are not negotiable — and only then does coverage count. **No language model is
involved**, so this stage costs nothing beyond computing time.

**Stage two — smoothing.** Takes an existing plan and evens it out: fairer distribution, fewer
awkward sequences. It deliberately **does not touch coverage decisions** — who works at all was
settled in stage one. Also no language model.

**Stage three — polishing.** Reviews the smoothed plan with a language model that reads the plan
**as a picture**, with several independent judgements weighed against each other. This is the only
stage that consumes model usage, so it is meant to be used sparingly. It needs a model that can
process images; without one configured, this stage is effectively switched off.

Run one after another, the three form the chain behind the autofill button in the schedule header.

## Running one stage alone

Each stage can be started on its own, which is worth knowing:

- Only stage one when a first plan is needed and neither smoothing nor a language model is wanted.
- Only stage two on an existing plan that is complete but uneven.
- Only stage three on an already smoothed plan, when the extra polish is worth the cost.

Stages two and three always work on the result of the stage before, so they need a plan to start
from — they cannot create one.

## While it runs

A run does not block the screen: it is handed off and reports back when finished. Its progress can
be polled at any time, and a run can be cancelled while in flight.

## What this is not

This describes the **machinery**. That the result is a proposal the planner has to accept, and never
a change made behind their back, is the subject of `explain_planning_assistant`.

## Related skills

- `start_autowizard` — the whole chain, as the autofill button triggers it
- `start_wizard1` / `start_wizard2` / `start_wizard3` — a single stage
- `list_open_wizard_jobs` — is a run still going?
- `cancel_wizard_job` — stop a running one

## Trigger phrases

- "How does the automatic planning actually work?"
- "Does auto-planning use AI?"
- "Which stage do I need if the plan is already full?"
- "Stop the run."
- "Warum kostet das etwas?"
