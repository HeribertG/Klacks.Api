---
name: explain_planning_wizards
description: |
  Explains the machinery behind automatic planning: three stages that run one after another. Stage
  one fills the plan with a genetic algorithm and no language model, stage two smooths the result
  without touching coverage, stage three polishes it with a deterministic local search; the earlier option
  to run stage three on a language model has been retired. Covers what each
  stage does, when to run one alone, how long a run takes and how to stop it. Use this when the user asks
  how auto-planning works internally, which stage to use, why a run is slow or costly, or how to cancel one.
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

Automatic planning is not one step but three; none of them needs a language model.

## The three stages

**Stage one — filling.** Builds the initial plan and decides who works which shift. It runs a
**genetic algorithm**: thousands of plan variants are generated, scored and recombined until a
balanced one stands. Hard rules always win — working-time law, rest periods, the limits on hours per
week and per day, qualifications and availability are not negotiable — and only then does coverage count.

Duties that already exist in the planned range, unlocked ones included, count as working days for the
limit on days in a row (de: "Max. aufeinanderfolgende Arbeitstage", en: "Max. Consecutive Work Days",
fr: "Max. jours de travail consécutifs", it: "Max. giorni lavorativi consecutivi"), just as they count
for overlaps, the hours off between two working days and the weekly rest days. A second run over a
range that is already partly filled therefore plans more cautiously: it may leave a slot open rather
than push somebody past that limit.

For qualifications the hard part is a missing mandatory qualification and a level below the minimum
where that level is binding; a level below a non-binding minimum is allowed but penalised by the size of
the gap, so people at the required level are preferred (see `explain_qualifications`). That penalty
exists **only in this stage**: stages two and three respect the same hard qualification rules, but they
do not weigh a gap below a non-binding minimum, so a swap there can put a less qualified person on a
duty. An assignment that already exists is not blocked afterwards because a level was later made
binding; it counts as a gap below a non-binding minimum. **No language model is involved**, so this
stage costs nothing beyond computing time.

**Stage two — smoothing.** Takes an existing plan and evens it out: fairer distribution, fewer
awkward sequences. It deliberately **does not touch coverage decisions** — who works at all was
settled in stage one. It keeps every hard limit, the hours per week and per day included. Also no language model.

**Stage three — polishing.** Reworks the smoothed plan as a whole. It runs a
**deterministic local search**: it tries swaps and pairs of swaps, keeps only those that make the plan
more harmonious without breaking a hard rule and without moving anyone further from their target
hours, and gives the same result for the same input. A swap between two different days is checked
against the rest between duties, the days in a row and the hours per week and per day as well. No language model is involved and it costs only
computing time. Harmony is computed, not judged by AI. The earlier option to let a language model read the
plan as a picture has been retired because measurements showed no gain. There is no setting for stage
three any more: it always computes deterministically, so there is nothing to switch to a language model or to
another model. If stage three fails, the chain keeps
the plan of stage two.

Run one after another, the three form the chain behind the autofill button in the schedule header.
Autofill is planning work: everyone who may edit the schedule may start it, follow it and cancel it, a
planner without a role included. A planner who sees only some groups plans only a group they see and
only the people and shifts they see, and does not see or cancel the runs of other groups.

## Who gets planned, and on which days

Every stage plans a person only between their entry and exit date (de: "Eintrittsdatum" /
"Austrittsdatum"). On those days the working weekdays and the hours aimed for come from the person's
active contract or, on a day without one, from the company-wide defaults in the settings. The
shift-work flag from the same source (whether duties other than early ones may go to the person) is applied
by all three stages and by the replacement search: a person without shift work only gets early duties. Late or
night duties such a person already holds are not cleared away; the planners only prevent new ones. An employee without any contract is therefore planned like everyone else; small companies
often keep no contracts at all. A placeholder (a row for a person who is not in the system yet) follows
the same company defaults, the hour limits included, but has target hours 0, and every stage puts every placeholder behind every real
person, also behind one with 0 target hours: the filling stage ranks every real person who can legally take a
service before a placeholder, and no later stage gives a placeholder a service a real person may legally keep in
exchange for a free day (swapping one service for another stays possible); only a service that breaks a rule for that
person may go to a placeholder for a free day (`explain_placeholder_employees`). A booked absence, an availability that leaves no usable hours and a
missing mandatory qualification keep a person off a day. When somebody stays unplanned, check in this
order: entry and exit dates, the ticked weekdays of the contract or the defaults, availability,
absences, qualifications.

## Who comes first, and what never gives way

**The order of the people decides who is served first.** The person at the top gets the guaranteed hours
first; the person at the bottom takes what is left. All three stages use the same order. It comes from one of
two places:

- **The planner's own order.** An order arranged by hand in the schedule is kept unchanged by all three stages.
  From the autofill button this needs the sort mode (de: "Individuell Sortiert", en: "Individual Sort") to be
  switched on and a saved order for the group that ranks at least one of the people shown. In the chat with
  Klacksy the saved order of the person who asks is enough, whether or not the sort mode is on. People the saved
  order does not mention follow behind the ranked ones.
- **The automatic order.** Otherwise people are ranked by the hours their contract guarantees, highest first,
  converted to the planned range: with one week planned, 40 hours a week rank above 100 hours a month. Somebody
  who joins or leaves inside the range counts only the days of employment. Ties, and people without guaranteed
  hours, are ranked by their full-time hours, highest first. Where pay periods are defined individually, the
  calendar month stands in for the conversion.

Placeholders always come last, also behind an order made by hand.

Whose order applies depends on who starts the run. The autofill button and a request to Klacksy in the chat use
the planner's own order as described above, and the automatic order when there is none. Planning that
Klacksy starts on its own for the next period, an external AI that works over the MCP interface, and Klacksy's
own target plan always use the automatic order. A saved order belongs to one user and one group, so two planners
can have different orders for the same group.

**Hours per week and per day are hard limits in every stage.** The limit is the one from the person's working
conditions (de: "Max. Wochenstunden", en: "Max. Weekly Hours"), else the company default, else 50 hours a week;
the daily limit works the same way and falls back to 10 hours a day when nothing is set (see `explain_scheduling_rules_model`).
When the working conditions only start inside the planned range, every stage takes the person's weekly and daily
limit from the first day that has active working conditions, not from the first day of the range; on each day the daily
limit of the working conditions valid that day applies as well. A week runs
Monday to Sunday, and a duty counts with its paid hours in the week of the day it starts. Absences and breaks
never count towards the limits. Duties that already exist in the same week count together with the new ones,
locked ones included and also when they lie just before or after the planned range, so a week that starts in the
previous month counts those days too.

**What wins when requirements collide**, from the top:

1. Hard limits never break to fill a slot. A duty that would break one stays open and shows up as an open slot
   in the result.
2. Coverage: among the plans that keep every hard limit, the one with more filled slots wins.
3. Restricting wishes (FREE, only-X, no-X): a veto in the strict run; in the second run they can be broken to fill
   an open slot, never to reach hours (see `explain_shift_type_preferences`).
4. Guaranteed hours, served by rank from the top.
5. Positive wishes (-FREE, EARLY, LATE, NIGHT): they attract a duty but give way to the guaranteed hours of other
   people.
6. Full-time hours, then evenness.

The rank settles conflicts: where two people compete for the same duty, the one higher in the order is served
first. The rotation of early, late and night duties is aimed at for everybody in the plan. Weekend duties are not
yet evened out in the first stage.

## Running one stage alone

Each stage can be started on its own, which is worth knowing. Starting a single stage is for
administrators only; everybody else uses the whole chain:

- Only stage one when a first plan is needed and no smoothing is wanted.
- Only stage two on an existing plan that is complete but uneven.
- Only stage three on an already smoothed plan that should be polished once more.

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
