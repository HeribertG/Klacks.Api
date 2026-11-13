---
name: explain_planning_wizards
description: |
  Explains the machinery behind automatic planning: the autofill chain runs stage one, which fills the
  plan with a genetic algorithm, and then stage three, which polishes it with a protected deterministic
  local search; neither uses a language model. Stage two (smoothing) is no longer part of the chain and
  exists only as a single stage for administrators. Covers what each stage does, what stage three
  protects, when to run one alone, how long a run takes and how to stop it. Use this when the user asks
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

# The planning assistants — stage one and stage three, one chain

## Core idea (one sentence)

Autofill fills the plan in stage one and polishes it in stage three; no stage needs a language model.

## The stages

**Stage one — filling.** Builds the initial plan and decides who works which shift. It runs a
**genetic algorithm** (plan variants are generated, scored and recombined). Hard rules always win — working-time law, rest periods, the limits on hours per
week and per day, qualifications and availability are not negotiable — and only then does coverage count.

Duties that already exist in the planned range, unlocked ones included, count as working days for the
limit on days in a row (de: "Max. aufeinanderfolgende Arbeitstage", en: "Max. Consecutive Work Days",
fr: "Max. jours de travail consécutifs", it: "Max. giorni lavorativi consecutivi"), for overlaps, the hours
off between two working days and the weekly rest days, so a second run over a partly filled range may leave
a slot open rather than break that limit.

A place on a shift that somebody already holds with a fixed duty (locked, corrected, handed over in a replacement or
moved by an absence cover) counts as staffed, also when that person is not one of the people the run plans, for example
a colleague from a sister group who took the duty over. Stage one does not staff that place a second time: with two
places on the shift and one held that way, it fills only the other one. Only fixed duties are counted this way.

A duty a person already has in another group (a group outside the run, for example a night duty in a second group
the person belongs to) stays in the plan and is respected by every stage, locked or not. Stage one does not give the
person a duty that overlaps it or breaks the rest before or after it, and it counts its hours towards the weekly limit.
Stage three, and stage two when an administrator runs it alone, treat that day of the person as taken: no swap puts
a second duty on it, and its hours count towards the person's hours.

For qualifications the hard part is a missing mandatory qualification and a level below the minimum
where that level is binding; a level below a non-binding minimum is allowed but penalised by the size of
the gap, so people at the required level are preferred (see `explain_qualifications`). That penalty
exists **only in this stage**: stages two and three respect the same hard qualification rules, but they
do not weigh a gap below a non-binding minimum, so a swap there can put a less qualified person on a
duty. An assignment that already exists is not blocked afterwards because a level was later made
binding; it counts as a gap below a non-binding minimum.

**Stage three — polishing.** Reworks the plan of stage one as a whole. It runs a
**deterministic local search**: it tries swaps and pairs of swaps, keeps only those that make the plan
more harmonious without breaking a hard rule and without moving anyone further from their target
hours, and gives the same result for the same input; it has no setting. A swap between two different days is
checked against the rest between duties, the days in a row and the hours per week and per day as well.

Stage three also protects what stage one achieved. It keeps a change only when, for the people it touches,
nobody loses hours to somebody ranked below them; no work package (days in a row with duties) is cut up; nobody
gets more packages, more short ones (one or two days), more longer than five days or more pairs with less than
48 hours of rest between them; and mixed packages (several kinds of duty in one block) do not rise over those
people together. A package running in from before the planned range counts with its full length. So stage three
changes only a few assignments; if it cannot improve the plan, or fails, the chain keeps the plan of stage one.

**Stage two — smoothing (not in the chain).** Evens out a plan without touching coverage. Autofill skips it
since 2026-11-12: measured, it moved hours from the top of the order downwards and added short packages; stage
three directly after stage one did better on both. Administrators can still run it alone.

Stage one followed by stage three is the chain behind the autofill button in the schedule header.
Autofill is planning work: everyone who may edit the schedule may start it, follow it and cancel it, a
planner without a role included. A planner who sees only some groups plans only a group they see and
only the people and shifts they see, and does not see or cancel the runs of other groups.

## Who gets planned, and on which days

Every stage plans a person only between their entry and exit date (de: "Eintrittsdatum" /
"Austrittsdatum"). On those days the working weekdays and the hours aimed for come from the person's
active contract or, on a day without one, from the company-wide defaults in the settings. The
shift-work flag from the same source (whether duties other than early ones may go to the person) is applied
by every stage and by the replacement search: a person without shift work only gets early duties. Late or
night duties such a person already holds are not cleared away; the planners only prevent new ones. An employee without any contract is therefore planned like everyone else; small companies
often keep no contracts at all. A placeholder (a row for a person who is not in the system yet) follows
the same company defaults, the hour limits included, but has target hours 0, and every stage puts every placeholder behind every real
person, also behind one with 0 target hours: the filling stage ranks every real person who can legally take a
service before a placeholder, and no later stage gives a placeholder a service a real person may legally keep in
exchange for a free day (swapping one service for another stays possible); only a service that breaks a rule for that
person may go to a placeholder for a free day (`explain_placeholder_employees`). A booked absence, an availability that leaves no usable hours and a
missing mandatory qualification keep a person off a day. A person who is only borrowed into the group for single
days (a cover from a sister group) is not planned by the autofill or the wizards of that group, also not when they
show in its schedule; they are planned in their home group (`explain_absence_cover_move`). When somebody stays
unplanned, check in this order: entry and exit dates, the ticked weekdays of the contract or the defaults,
availability, absences, qualifications, and whether the person is only borrowed into the group.

## Who comes first, and what never gives way

**The order of the people decides who is served first.** The person at the top gets the guaranteed hours
first; the person at the bottom takes what is left. Both stages of the chain use the same order (stage two does too when run alone). It comes from one of
two places:

- **The planner's own order.** An order arranged by hand in the schedule is kept unchanged by both stages of the chain.
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
yet evened out in the first stage. Where a shift cycle fits the team, the first stage also tries a staircase plan as a second
run and keeps the better plan by this order (see `explain_autofill_staircase_start`).

## Running one stage alone

Starting a single stage is for administrators only; everybody else uses the chain. Stage one alone gives a
first plan without polishing; stage two or three alone works on an existing plan and cannot create one.

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
