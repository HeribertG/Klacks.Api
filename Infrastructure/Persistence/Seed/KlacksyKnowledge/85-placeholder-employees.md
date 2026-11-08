---
name: explain_placeholder_employees
---

# Placeholders: planning rows for people who are not in the system yet

A placeholder (de: "Platzhalter", en: "Placeholder", fr: "Employé fictif", it: "Segnaposto") is a planning row for a person who is not known yet: it is planned like an employee and later converted into the real employee or external employee.

<!-- level:short -->

## What placeholders are for

Sometimes the work is known but the people are not: a large order needs extra staff that is still being recruited, or an agency will send the names later. Placeholders let the planner staff the shifts now and put the real names in afterwards. A placeholder is **not** an external employee; employee and external employee are the two things a placeholder can become.

Placeholders are for **uncertain people**; keys are for **uncertain shifts** (`explain_scenario_variant_keys`). Both together let a scenario for an announced large order be fully staffed before anything is confirmed.

Do not mix it up with two other things:
- The **stand-in** in a replacement (German "Ablöser") is the real person who takes over part of somebody's shift (`explain_overtime_model`, `explain_payroll_export_handoff`). A placeholder is never a stand-in.
- In the absence calendar a planned absence wish is also called a placeholder in German ("Absenz-Platzhalter"). That is a wish bar for an existing person and has nothing to do with placeholder rows.

<!-- level:elements -->

## Creating placeholders

Only a Supervisor or an Admin creates placeholders (the right to create employees); an account without a role cannot. One request creates 1 to 50 placeholders, all of them or none:
- **Count** — 1 to 50.
- **Name pattern** — must contain `{n}` exactly once, e.g. "Fair {n}" gives "Fair 1", "Fair 2" …; numbering continues above the highest number an existing name with the same pattern already has.
- **Group** — required, and it must be a group the user can see.
- **Period** — required start, optional end. It becomes the placeholder's membership and its group membership, so the placeholder can only be planned inside it.
- **Qualifications** — optional; every given qualification is held at the highest level, Expert, so the placeholder can take every shift that requires it.

A placeholder has only a display name: no first name, no gender, no address, no e-mail or phone and no contract. The client import never creates placeholders. Klacksy has no skill for creating or converting placeholders; it explains the fields and the rules.

## Finding placeholders

- Employee list: in the dropdown (de: "Typ", en: "Type", fr: "Type", it: "Tipo") the checkbox (de: "Platzhalter", en: "Placeholder", fr: "Employé fictif", it: "Segnaposto").
- Schedule and timeline: the filter checkbox (de: "Platzhalter", en: "Placeholders", fr: "Employés fictifs", it: "Segnaposto") next to the employee and external checkboxes.
- The row header marks a placeholder with the symbol ◌ and the tooltip (de: "Platzhalter: steht für eine Person, die noch nicht erfasst ist", en: "Placeholder: stands for a person who is not in the system yet").
- The search field in the header does not find placeholders; tick the checkbox in the employee list instead.

## Planning on placeholders

A planner plans on existing placeholders like on any person: place, move and remove services, also by drag and drop; automatic planning treats them as planning rows as well. A placeholder follows the company defaults of an employee without a contract (rest time, maximum hours, days in a row), but its **target hours are 0**. The first stage of automatic planning (Wizard 1, also as the first stage of autofill) puts every placeholder behind every real person, so real people are planned first and placeholders take what is left.

## Converting a placeholder into a person

Only a Supervisor or an Admin converts (the right to edit employees). The same record becomes an employee or an external employee; the other way round is impossible, and no person can be turned into a placeholder, neither in the employee form nor through Klacksy.
- **Required:** target type (de: "Mitarbeiter", en: "Employee", fr: "Employé", it: "Dipendente") or (de: "Externer", en: "External", fr: "Externe", it: "Esterno"), first name, name (it replaces the display name) and gender.
- **Optional:** an address (of a kind the target type allows) and e-mail or phone; both can also be added later in the normal employee form, like a contract.
- **Kept:** the id and staff number, every service, the membership, the group memberships and the qualifications.
- **Afterwards** the person's target hours come from the contract or the company defaults, all placeholder locks are gone, and the hours and time credits of the person's services in the real plan are recalculated in the background (scenarios are not).

<!-- level:effects -->

## What is locked while services lie on a placeholder

- **Period close:** a period (global or for a group) cannot be closed while one of its services lies on a placeholder. This check comes once every service is confirmed, it cannot be confirmed away with (de: "Trotzdem abschliessen", en: "Close anyway"), and (de: "Probleme dieser Periode", en: "Issues in this period", fr: "Problèmes de cette période", it: "Problemi di questo periodo") lists it. Klacksy's automatic close does not close such a period either.
- **Payroll export:** blocked with the reason (de: "Dienst liegt auf einem Platzhalter", en: "Service assigned to a placeholder", fr: "Service attribué à un employé fictif", it: "Turno assegnato a un segnaposto"), one per placeholder and day, also when a real person replaces part of such a service.
- **Deleting:** only an Admin deletes, and a placeholder that still holds services of the real plan cannot be deleted; the message names the placeholder and the number of services. Services that exist only in scenarios do not block the deletion.
- **No contact:** no address, no e-mail or phone, no (de: "Dienstplan versenden", en: "Send Staff Schedule", fr: "Envoyer le planning du personnel", it: "Invia piano turni") and no place in a broadcast.
- **No replacement:** a placeholder is never offered or accepted as a replacement or cover, and its row has no (de: "Ausfall decken...", en: "Cover absence...", fr: "Couvrir l'absence...", it: "Copri assenza...").
- **Not a head:** the employee count of the period summary, the resource monitor and the target-hours drift leave placeholders out. The absence calendar and the availability page do not list them.

<!-- level:pitfalls -->

## Typical course with keys, and what to watch

1. Put the uncertain shifts of the announced order into a key (`explain_scenario_variant_keys`).
2. Create a scenario that carries the key.
3. Create placeholders for the missing people: the group of the shifts, the period of the order, the qualifications the shifts need.
4. Plan the scenario automatically: its first stage plans real people first, placeholders fill the rest.
5. When the customer confirms, accept the scenario (Supervisor or Admin); its services, including those on placeholders, become the real plan.
6. As names arrive, convert each placeholder, or move its services to an existing person one by one (drag and drop); the move checks membership, group period and mandatory qualifications. A transfer of all services at once does not exist.
7. Before the period close no service may be left on a placeholder.

- **Placeholders are not bound to a scenario.** Discarding the scenario keeps them; an Admin deletes the ones no longer needed.
- **Qualification level after converting.** The person keeps the placeholder's qualifications at level Expert; adjust them in (de: "Qualifikationen", en: "Qualifications") to the real person.
- **Same pattern at the same moment.** Two requests with the same name pattern at the same time can produce equal names; the placeholders stay distinct.

### Trigger phrases

- "Wie plane ich Dienste, wenn die Leute noch fehlen?"
- "What is the dotted circle in the row header?"
- "Warum kann ich die Periode nicht abschliessen, es liegen Dienste auf Platzhaltern?"
- "How do I turn a placeholder into a real employee?"
