---
name: explain_autofill_staircase_start
---

# Staircase start: stage one tries a shift cycle as a second plan

In its first stage the autofill can plan twice: once exactly as before and, when a staircase (German "Treppenmuster") fits the team, a second time starting from a staircase plan. The usual judge keeps the better plan, so the result is never rated worse than the first run's.

<!-- level:short -->

## What the staircase is

The top of the employee order works one shared cycle, each row shifted against the next. One stage is one week: as many work days as (de: "Max. Arbeitstage", en: "Max. Work Days", fr: "Max. jours de travail", it: "Max. giorni lavorativi") allows, at most 7 minus the rest days between two work blocks, then free days; one stage per needed shift kind, early, late, night. The scarcest shift kind decides how many rows there are; the rows go to the top ranks, the others are fillers and take the rest. A person who worked in the 14 days before the period continues that rhythm where it fits beside the higher rows, otherwise becomes a filler; after at least 7 days of absence the row restarts with early shifts (late if early is not possible, else night).

<!-- level:effects -->

## When it is tried, and what it costs

Only when every person on a row can work every needed shift kind, each kind is needed every day, there are at least two rows, a row fits the contract hours and the period is at least one cycle long. The judge decides in its usual order (`explain_planning_wizards`). With a time limit the second run only starts if the first used at most half of it; otherwise stage one takes about twice as long. When the staircase wins, packages are more regular, but lower ranks can get fewer hours and weekends can be spread less evenly, because each stage is exactly one week and keeps a row's free days on the same weekdays.

## Trigger phrases

- "Why does the plan look like a staircase?"
- "Warum arbeiten die oben immer 5 Tage, dann 2 frei?"
- "What is a filler in the plan?"
