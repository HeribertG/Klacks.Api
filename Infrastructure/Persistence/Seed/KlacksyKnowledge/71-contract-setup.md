---
name: explain_contract_setup
description: |
  Interview guide for setting up a new employment contract with the administrator: what to extract from
  the request first, which questions are still open (payment interval, fixed guaranteed hours or a
  workload percent of the company-wide value, holiday-calendar region, start date, name, working
  weekdays, shift work), how to compare existing contracts, and how the new contract is created as an
  adapted copy of a fitting template. Use this when somebody wants a new contract or asks which
  contract fits a kind of employment.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - welchen vertrag brauche ich
    - vertrag einrichten
    - neuen vertrag
    - brauche einen vertrag
  en:
    - which contract do i need
    - contract setup
    - need a contract
    - a new contract
synonyms:
  de: [welchen vertrag brauche ich, vertrag einrichten, neuen arbeitsvertrag aufsetzen, vertragsberatung für neue mitarbeiter]
  en: [which contract do I need, set up a contract, contract setup advice, help me define a new employment contract]
  fr: [quel contrat me faut-il, mettre en place un contrat, aide pour définir un nouveau contrat de travail]
  it: [di quale contratto ho bisogno, impostare un contratto, aiuto per definire un nuovo contratto di lavoro]
---

# Contract setup — the interview before a new contract exists

## Core idea (one sentence)

A new contract is almost never built from nothing: every existing contract is a **template**, so the
fastest correct route is to find the closest one and create the new contract as a copy with only the
differing values adapted.

## Step 1 — take over what was already said

Read the request first and extract every fact it contains. "Part time 80 % Zurich with shift work"
already answers workload, region and shift work. Ask only for what is still missing, **at most one or
two questions per turn**, and never invent a value.

Never turn a word such as "full time" or "part time" into hours. Hours come from the administrator as
numbers, or from a percent of the company-wide value.

## Step 2 — the facts that decide the contract

**Payment interval** — weekly, every two weeks, monthly, individual or monthly target hours.
*Individual* only works when the template already carries a custom pay period, and that period is copied
only while the interval stays *Individual*: any other interval drops it. *Monthly target hours* makes the
company-wide table of hours per calendar month decide the hours of every month that has a row there,
scaled by the percent; fixed guaranteed hours apply only in months without a row in that table.

**Workload — exactly one of two paths:**

1. **Fixed hours** — guaranteed hours per interval, optionally minimum, maximum and full-time hours.
   Minimum and maximum default to the guaranteed hours, so a band around it must be asked for.
2. **Inherited workload** — no guaranteed hours at all; the contract takes the company-wide value
   scaled by a percent (100 when none is given).
   Describe its guaranteed hours as "inherited from the company-wide value", never as 0 — only the
   minimum and maximum band is 0 on this path.

**Holiday-calendar region** — the state, canton or region code. It must match exactly one calendar of
the installation; with none or several matching, the real calendar names are offered instead.

**Valid from** — the start date. **Name** — must be new; a name that already exists is refused and no
suffix is added.

Optional: the working weekdays (they come from the template unless the administrator wants others) and
whether the work is shift work.

## Step 3 — compare, then let the administrator choose

With the facts known, compare the existing contracts with `evaluate_contract_templates`. It ranks the
three closest templates and lists, per template, exactly which wished values differ. It also lists the
**implied changes** of the workload path, which are no wishes but happen anyway: switching to a percent
drops the template's fixed guaranteed hours and resets its minimum/maximum band to 0; switching to fixed
hours drops the percent and collapses the band to the new guaranteed hours.

Present the best candidate with the differences **and** the implied changes, and ask **which template** to
use and whether the adapted values are right. Once the administrator has chosen, call
`create_contract_from_template` directly: the system itself asks for the final confirmation before
anything is written, so do not ask a separate "shall I create it?" first.

## Step 4 — create the copy

`create_contract_from_template` copies the template and applies only the adapted values.

**Copied unchanged:** the night, holiday, Saturday, Sunday and WE3 (third weekend day) time credits,
the night window, the scheduling rule, the holiday calendar (unless a region is given), the working
weekdays (unless others are given) and an individual pay period (only while the interval is unchanged).
The template's valid-until date is **not** copied, so the new contract is open-ended; copying an expired
template works but is reported.

Surcharges are **time credits, never money**, and never overtime: call them time credits for night,
holiday, Saturday and Sunday work. Afterwards the night, holiday, Saturday and Sunday credits, the
valid-until date, the working weekdays and the holiday calendar (by region) can be changed with
`update_contract`. The WE3 credit, the night window and the scheduling rule can only be changed in the
contract settings page.

If the copied contract keeps a scheduling rule and the administrator adapted guaranteed, minimum, maximum
or full-time hours or the working weekdays, the result warns that the rule may override those values:
a value the rule sets wins over the contract's own. Tell the administrator and point to the rule in the
contract settings page.

## Step 5 — no fitting template

If no template is close, or there are no contracts yet, create the contract from scratch with
`create_contract`, using the values gathered in the interview. That skill cannot set the working weekdays
or the holiday calendar; set them right afterwards with `update_contract` (weekdays as a list such as
Mon,Tue,Wed,Thu,Fri, the calendar by region code). The WE3 credit, the night window and the scheduling
rule still need the contract settings page.

## Step 6 — giving it to a person

Creating the contract does not assign it. Assigning it to an employee is a separate step with its own
start date: `assign_contract_by_name`.

## Trigger phrases

- "Which contract do I need for a part-time employee in Zurich?"
- "Set up a contract like Full time 160 but with 120 hours"
- "Welchen Vertrag brauche ich für eine Teilzeitstelle mit Schichtarbeit?"
- "Neuen Vertrag einrichten"
