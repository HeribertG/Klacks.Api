---
name: explain_absence_type_flags
description: |
  Explains what the switches on an absence kind actually do today. The Saturday, Sunday and
  public-holiday switches are stored but read by no calculation, so toggling them changes nothing.
  The unpaid switch can only be turned on together with two other switches; saving the kind on the
  settings page resets it otherwise. Once on, it deducts its span from the paid duration of the
  container service it sits inside. Use this when a counting switch has no visible effect or unpaid
  keeps unchecking.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - unbezahlt
  de:
    - samstag wird berechnet wirkung
    - unbezahlt lässt sich nicht ankreuzen
  en:
    - saturday is calculated has no effect
    - unpaid keeps unchecking
synonyms:
  de: [was bewirkt samstag wird berechnet, hat der feiertag schalter eine wirkung, warum hakt sich unbezahlt wieder ab, wofür ist gilt auch für container gut, was bedeutet der schalter unbezahlt bei einer absenzart, zieht eine unbezahlte absenz stunden vom dienst ab]
  en: [what does saturday is calculated actually do, does the holiday switch have any effect, why does unpaid uncheck itself again, what is also applies to container for, what does the unpaid switch on an absence kind do, does an unpaid absence reduce the paid hours of a shift]
  fr: [que fait vraiment le samedi est calculé, l interrupteur jour férié a-t-il un effet, pourquoi non payé se décoche-t-il tout seul, à quoi sert s applique aussi au container]
  it: [cosa fa realmente il sabato è calcolato, l interruttore festività ha un effetto, perché non pagato si deseleziona da solo, a cosa serve si applica anche al container]
---

# Absence-kind switches: some are decorative, some are real

A kind of absence carries several switches that look similar but behave differently: the counting switches do nothing today, while the others change what Klacks does.

<!-- level:short -->

## What the switches of a kind of absence do

Every kind of absence, such as holiday or sick leave, has a few switches in the settings. Some of them look as if they decide which days count, but the three for Saturday, Sunday and public holidays are only stored and change nothing. Other switches do work: one marks a kind as only for internal use, one makes it available inside container services, one marks it as unpaid there, and one marks on-call duty so that the person is suggested first as a replacement. Example: with the unpaid switch on, a two-hour unpaid absence inside an eight-hour container service leaves six paid hours.

<!-- level:elements -->

## The switches, step by step

1. Open the settings, section (de: "Abwesenheit & Kalender", en: "Absence & Calendar", fr: "Absences & Calendrier", it: "Assenze & Calendario"), card (de: "Absenzen", en: "Absences", fr: "Absences", it: "Assenze"). Click a kind to edit it, or (de: "+ Absenz hinzufügen", en: "+ Add Absence", fr: "+ Ajouter une absence", it: "+ Aggiungi assenza") for a new one.
2. The counting switches are (de: "Samstag wird berechnet", en: "Saturday is calculated"), (de: "Sonntag wird berechnet", en: "Sunday is calculated") and (de: "Feiertage werden berechnet", en: "Holidays are calculated"). Ticking them changes nothing today.
3. (de: "Gilt auch für Container", en: "Also applies to container", fr: "S'applique aussi au container", it: "Si applica anche al container") offers the kind inside container services.
4. (de: "Nur für den internen Gebrauch", en: "For internal use only", fr: "Uniquement pour usage interne", it: "Solo per uso interno") hides the kind from the absence calendar.
5. (de: "Unbezahlt", en: "Unpaid", fr: "Non payé", it: "Non pagato") appears only after both of the two switches above are ticked.
6. (de: "Pikett / Bereitschaft (bevorzugt beim Ausfall decken)", en: "On call / standby (preferred cover for short-notice absences)") marks on-call duty.
7. Save. The change applies to future bookings; existing bookings keep their values.

<!-- level:effects -->

## What each switch does to the rest of Klacks

### The counting switches: stored, not evaluated

(de: "Samstag wird berechnet", en: "Saturday is calculated"), (de: "Sonntag wird berechnet", en: "Sunday is calculated") and (de: "Feiertage werden berechnet", en: "Holidays are calculated") are stored on the kind and shown in its edit form. No calculation anywhere reads them, so ticking or clearing them changes nothing else.

### Internal use and container

(de: "Nur für den internen Gebrauch", en: "For internal use only") removes the kind from the legend and the diagram of the absence calendar. (de: "Gilt auch für Container", en: "Also applies to container") lists the kind in the dialogs of container services, where it can be booked as a sub-entry.

### The unpaid switch: gated, and it does something

(de: "Unbezahlt", en: "Unpaid") can only be on while both switches above are on. When a kind marked unpaid is booked inside a container service, the length of the unpaid span is subtracted from the paid working time that Klacks calculates for the container's parent entry, whenever the container's entries are saved. An eight-hour container service with a two-hour unpaid absence inside it is paid for six hours.

### On call

The on-call switch changes the replacement search and the checks: a person who holds such an absence is not treated as absent, is offered first as a replacement, and a shift on top of the on-call day is reported as an on-call overlap warning instead of a collision. The payroll export counts an on-call absence in days, not in hours.

### Related skills

- `create_absence_type`, `update_absence_type` — where these switches are set

### Trigger phrases

- "Was bewirkt der Schalter 'Samstag wird berechnet' bei einer Absenzart?"
- "Why does 'Unpaid' keep unchecking itself when I save an absence kind?"
- "Zieht eine unbezahlte Absenz Stunden vom Container-Dienst ab?"
- "Does the holiday switch on an absence type do anything?"

<!-- level:pitfalls -->

## Consequences and edge cases

**Unpaid is reset on the settings page, not everywhere.** When a kind is saved on the settings page with either of the two required switches off, the server sets "Unpaid" back to off. Klacksy's skills for creating and changing kinds set the switch without that check, and the deduction reads only the unpaid switch, so it works there as well.

**The deduction exists only inside container services.** Outside a container the unpaid switch has no effect on the hours. An unpaid span that is not fully inside the time span of the container is not deducted.

**The deduction is computed when the container is saved.** Switching "Unpaid" on later does not change the paid time of containers that are already saved until their entries are saved again.

**The on-call day stays occupied for the wizards.** The wizards still keep the person out of that day, and an on-call absence carries no hours by design, so it does not add to the worked hours.

**Retiring a kind.** To take a kind out of use without deleting it, tick (de: "Nur für den internen Gebrauch", en: "For internal use only"); deleting is refused while bookings or wishes use it.
