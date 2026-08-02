---
name: explain_absence_type_flags
description: |
  Explains what the switches on an absence kind actually do today. The Saturday, Sunday and
  public-holiday switches are stored but read by no calculation anywhere, so toggling them changes
  nothing. The unpaid switch can only be turned on together with two other switches and is reset
  otherwise; once genuinely on, it deducts its span from the paid duration of the container service
  it sits inside. Use this when a counting switch has no visible effect or unpaid keeps unchecking.
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

# Absence-kind switches: one set is decorative, one set is real

## Core idea (one sentence)

An absence kind carries two families of switches that look similar but behave completely
differently — one family currently does nothing, the other gates a real deduction of paid hours.

## The counting switches: recorded, not evaluated

Every absence kind has three switches — "Saturday is calculated", "Sunday is calculated", "Holidays
are calculated" (de: "Samstag wird berechnet", "Sonntag wird berechnet", "Feiertage werden
berechnet"). They read like they should decide whether those days are included when hours are
counted. Today, no calculation anywhere in the system reads them: they are stored on the absence kind
and shown in its edit form, nothing more. Ticking or clearing them changes nothing else.

## The unpaid switch: gated, and it does something

A separate switch, "Unpaid" (de: "Unbezahlt"), can only be turned on at the same time as two other
switches on the kind are both on: "For internal use only" and "Also applies to container" (de: "Nur
für den internen Gebrauch", "Gilt auch für Container"). Save the kind with either of those two off,
and the server resets "Unpaid" back off by itself — it never sticks halfway.

Once "Unpaid" is genuinely on, it has a real effect: when that absence kind is booked inside a
container service — several sub-services chained under one umbrella shift — the length of the unpaid
span is subtracted from the paid working time the server calculates for the container's parent
entry. An eight-hour container service with a two-hour unpaid absence inside it is paid for six.

## Related skills

- `create_absence_type`, `update_absence_type` — where these switches are set

## Trigger phrases

- "Was bewirkt der Schalter 'Samstag wird berechnet' bei einer Absenzart?"
- "Why does 'Unpaid' keep unchecking itself when I save an absence kind?"
- "Zieht eine unbezahlte Absenz Stunden vom Container-Dienst ab?"
- "Does the holiday switch on an absence type do anything?"
