---
name: explain_absence_value_vs_hours
description: |
  Explains why the VALUE shown for an absence entry in the calendar list is not the number of hours
  that later counts, even once booked. The displayed value is a plain days-times-default-rate figure
  that never excludes weekends or holidays, while the stored hours come from a separate calculation:
  a macro on the absence kind when one exists, or otherwise the value given at booking time. Use this
  when a displayed figure does not match the booked hours.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - makro
  de:
    - wert stimmt nicht mit stunden überein
    - woher kommen die absenz-stunden
  en:
    - value does not match hours
    - where do absence hours come from
synonyms:
  de: [wert spalte stimmt nicht mit den stunden überein, warum zeigt die liste eine andere zahl, rechnet der kalender wochenenden heraus, woher kommen die tatsächlichen stunden einer absenz, berechnet ein makro die absenz-stunden, stimmt der angezeigte wert mit der lohnabrechnung überein]
  en: [the value column does not match the hours, why does the list show a different number, does the calendar subtract weekends from the value, where do the actual absence hours come from, does a macro calculate the absence hours, does the displayed value match payroll]
  fr: [la colonne valeur ne correspond pas aux heures, pourquoi le calendrier affiche un autre nombre, le calendrier déduit-il les jours fériés de la valeur]
  it: [la colonna valore non corrisponde alle ore, perché il calendario mostra un numero diverso, il calendario sottrae i giorni festivi dal valore]
---

# The calendar value and the booked hours are two different numbers

The figure in the VALUE column of the absence calendar and the hours stored on the same entry are worked out independently, by two calculations that never talk to each other.

<!-- level:short -->

## Two numbers for one absence

In the list of the absence calendar, the VALUE column is a quick figure for the eye: the number of days of the entry times a rate that belongs to the kind of absence. It is not the number of hours that count for the person. The hours are worked out when the absence is booked, using the rules stored with the kind of absence. That is why the two numbers can differ for the very same entry; the hours are the ones that count. Example: a holiday from Thursday to Monday counts five days in the list, although two of them are a weekend.

<!-- level:elements -->

## Where you see each number, step by step

1. **The calendar value.** Open the absence calendar (de: "Absenzen Kalender", en: "Absence Calendar"), select the person's row and open the list tab (de: "Liste", en: "List"). The column (de: "WERT", en: "VALUE", fr: "VALEUR", it: "VALORE") shows days times rate. It stays empty when the rate of the kind is 0.
2. **The rate.** Open the settings, section (de: "Abwesenheit & Kalender", en: "Absence & Calendar"), card (de: "Absenzen", en: "Absences"), and edit the kind: the field is (de: "Berechneter Wert pro Tag", en: "Calculated Value per Day").
3. **The hours that count.** In the schedule the row header of the person shows the sum of the worked hours of the period (tooltip de: "Geleistete Stunden", en: "Worked Hours").
4. **The rule behind the hours.** In the edit form of the kind the dropdown (de: "Macro Liste", en: "Macro list") chooses the calculation macro. The dropdown appears only if macros exist.
5. **Fixed hours for one extended absence.** In the settings card (de: "Erweiterte Absenzen", en: "Extended Absences") an extended absence has the mode (de: "Zeitbereich", en: "Time Range") or (de: "Dauer", en: "Duration"). In the mode Duration the hours are entered directly.
6. **After changing a rule.** Use the recalculation in the schedule toolbar (de: "Stunden berechnen und zusammenzählen", en: "Calculate and sum hours", fr: "Calculer et totaliser les heures", it: "Calcola e somma ore") so existing bookings get the new hours.

<!-- level:effects -->

## How the two numbers are produced

### The displayed value

For every row of the list, wish or booked absence alike, the calendar counts the days of the entry (both ends included) and multiplies them by the rate of the kind. It is computed on the fly for display only and never saved. It does not subtract weekends or public holidays: a five-day entry over a weekend is "5 × rate". If the rate is 0, the cell stays empty.

### The stored hours

When an absence is booked or changed, every booking path (single entry, several days at once, change) runs the same check. If the kind has a calculation macro, the macro computes the hours of that day from the day's real context — the regular working hours of the person's contract, the weekday, the public holidays and the start and end time — and replaces the hours the booking started with. If the kind has no macro, the hours stay whatever was supplied. If the macro fails, the supplied hours stay and nothing is shown to the planner.

What is supplied depends on the way of booking: the schedule grid supplies 0 hours for a plain absence and the chosen duration for an extended absence in the mode Duration; Klacksy's `add_break` supplies eight hours unless it is told otherwise.

### Where the hours go

The hours of all absences, work entries and corrections of a person are summed per period and compared with the guaranteed hours; the sum is updated whenever an entry is written. So the stored hours, not the calendar value, drive the hours balance and the payroll figures.

### Related skills

- `add_break`, `update_break` — booking, where the macro (if any) computes the real hours
- `list_absence_types` — shows an absence kind's default rate and whether it has a macro

### Trigger phrases

- "Warum stimmt der Wert in der Liste nicht mit den Lohnstunden überein?"
- "Does the calendar figure already exclude weekends?"
- "Woher kommen die tatsächlichen Stunden einer gebuchten Absenz?"
- "Why does a booked absence show one number but pay a different one?"

<!-- level:pitfalls -->

## Consequences and edge cases

**The two numbers can disagree permanently.** The calendar list keeps showing days times rate even for an already booked row; it never reads the stored hours. Look at the schedule, not at the VALUE column, to judge the hours.

**A kind without a macro books 0 hours from the grid.** A kind you create yourself starts without a macro until you choose one in its edit form. Booked in the schedule grid, such an absence carries 0 hours unless it is an extended absence in the mode Duration, so it adds nothing towards the guaranteed hours.

**A directly entered duration is left alone.** An entry with equal start and end time and hours above 0 counts as having a directly recorded duration; the macro does not overwrite it. An entry with equal times and 0 hours is treated as empty and does go through the macro.

**Booking a wish writes one entry per calendar day.** Weekends and public holidays included; the macro of the kind then decides the hours of each day separately.

**Changing the rule later does not change existing bookings.** The recalculation (de: "Stunden berechnen und zusammenzählen") runs the macros again for the entries that are still open, and skips every entry that is confirmed, approved or closed. To correct such a day, reopen it first.

**The Saturday, Sunday and holiday switches of the kind do not take part.** They are stored but no calculation reads them, so they cannot make the calendar value or the hours skip a weekend.
