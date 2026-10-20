---
name: explain_membership_single_row
description: |
  Explains that a person's membership is a single row rather than a history: a departure and a
  later return overwrite the same entry's dates instead of adding a new one. Also covers the Type
  dropdown on the same card, which changes the person's own classification (employee, external,
  customer) and not the membership. Use this when asked whether a re-entry keeps the old start
  date, or what the Type control on that card changes.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - membership
  de:
    - mitgliedschaft
    - wiedereintritt
  en:
    - membership
    - re-entry
synonyms:
  de: [mitgliedschaft hat keine historie, wiedereintritt überschreibt eintrittsdatum, alte mitgliedschaft geht verloren, typ in der registrierung ändert die person, nur eine mitgliedschaftszeile pro person, mitgliedschaft wird überschrieben statt neu]
  en: [membership has no history, re-entry overwrites entry date, old membership period lost, type dropdown in the registration card, only one membership row per person]
  fr: [adhésion sans historique, réintégration écrase la date d'entrée, le type dans la carte inscription change la personne]
  it: [appartenenza senza storico, rientro sovrascrive la data di ingresso, il tipo nella scheda registrazione cambia la persona]
---

# Membership — one row, no history

A person has exactly one membership record (their entry and exit dates), never a list of past periods,
so a later return overwrites the same entry instead of adding to it. This is not the person's link to
a group.

<!-- level:short -->

## One entry and one exit per person

A person has one "registration" with an entry date and, once they have left, an exit date. There is
only ever one such pair: if somebody leaves and later comes back, the same entry is changed — the new
entry date replaces the old one. Example: Anna joins on 1 March 2023, leaves on 30 June 2024 and rejoins
on 1 September 2025. Afterwards her entry date reads 1 September 2025; the March 2023 date is gone. So
if the exact first period matters later, write it down somewhere else before the person returns.

<!-- level:elements -->

## Where to see and change it

1. Open the person's own page and find the card (de: "Registration", en: "Registration", fr: "Inscription",
   it: "Registrazione").
2. The dates are the fields (de: "Eintrittsdatum", en: "Entry Date", fr: "Date d'entrée", it: "Data di
   ingresso") and (de: "Austrittsdatum", en: "Exit Date", fr: "Date de sortie", it: "Data di uscita").
   Recording an exit means filling the exit date of this one record.
3. In the same card sits the dropdown (de: "Typ", en: "Type", fr: "Type", it: "Tipo") with the choices (de:
   "Mitarbeiter", en: "Employee", fr: "Employé", it: "Dipendente"), (de: "Externer", en: "External", fr:
   "Externe", it: "Esterno") and (de: "Kunde", en: "Customer", fr: "Client", it: "Cliente"). It changes the
   person's classification — not the membership.
4. Klacksy: `list_client_memberships` shows the record, `update_membership` changes its dates,
   `end_client_membership` sets the exit date.

<!-- level:effects -->

## What the entry and exit dates drive

The schedule lists only people who are not customers and whose entry/exit window overlaps the period
shown. Within the period, days before the entry date and after the exit date are locked in the person's
row. Entries that were already planned after a newly set exit date stay in the database; they are
neither deleted nor flagged. The person's links to groups are separate records with their own dates (see the group membership
validity).

The membership record holds only these dates. Whether somebody counts as staff, external staff or a
customer is the person's own classification, a separate field.

### Related skills

- `list_client_memberships` / `update_membership` / `end_client_membership` — reading and changing
  the one membership record
- `update_client_type` — changing the person's classification

### Trigger phrases

- "Wenn jemand wieder eintritt, sehe ich dann noch das alte Eintrittsdatum?"
- "Was ändert das Feld Typ in der Karte Registrierung?"
- "Does re-joining keep the previous membership period?"
- "Does the Type dropdown in the registration card change the membership?"

<!-- level:pitfalls -->

## What cannot be recovered

**The earlier period is gone for good.** There is only ever a single membership per person, holding one
start date and one optional end date. Recording an exit sets the end date on that same record; a later
re-entry replaces its start date. Nothing archives the earlier dates, and no second entry exists to
preserve them. If the exact history of an earlier stay matters, it has to be recorded outside the
membership before the rejoin overwrites it.

**Changing the type control changes the person, not the membership.** The control that changes the
classification sits visually inside the membership card, right next to the dates, which makes it easy to
assume it edits the membership. It edits the person's classification directly; the membership itself has
no type and keeps only its dates.

**A classification change can be refused.** For a person marked as a legal entity the type is locked to
customer; see the legal entity rule.
