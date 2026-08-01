---
name: explain_calendar_model
description: |
  Explains how public holidays are defined and which ones apply. Covers the notation behind a
  holiday date — a fixed day such as 01/01, a date pulled onto a given weekday, and dates counted
  from Easter — plus the shift applied when a holiday lands on a weekend. Covers named bundles that
  gather the regions whose holidays should count, why a regional bundle must list its national
  entries as well, and how a holiday can be shown as a reminder without counting towards pay. Use
  this when the user asks why a day is not marked red, how a movable feast is entered, or where a
  bundle takes effect.
category: Query
executionType: Skill
alwaysOn: false
parameters:
  - name: level
    type: enum
    required: false
    enumValues: [short, elements, effects]
triggerKeywords:
  mul:
    - easter
  de:
    - feiertag
    - feiertagsregel
    - kalenderauswahl
    - ostern
    - beweglicher feiertag
    - kanton feiertag
  en:
    - public holiday
    - holiday rule
    - calendar selection
    - movable feast
synonyms:
  de: [feiertag, feiertagsregel, feiertagskalender, kalenderauswahl, ostern, karfreitag, beweglicher feiertag, kantonaler feiertag, warum ist der tag nicht rot, feiertag zählt nicht]
  en: [public holiday, holiday rule, holiday calendar, calendar selection, easter, movable feast, regional holiday, why is that day not marked, holiday does not count]
  fr: [jour férié, règle de jour férié, calendrier des jours fériés, sélection du calendrier, pâques, fête mobile, jour férié cantonal]
  it: [giorno festivo, regola festività, calendario festività, selezione calendario, pasqua, festa mobile, festività cantonale]
---

# Holidays — how a date is defined and which ones count

<!-- level:short -->

## Stage 1 — Two separate things

**A holiday rule** (de: "Feiertagsregeln", en: "Holiday rules", fr: "Règles des jours fériés",
it: "Regole per le vacanze") describes *when* a holiday falls, for one country and one region. It
carries a name in four languages, the notation for the date, an optional shift when it lands
awkwardly, and a marker for whether it is an official holiday.

**A calendar selection** (de: "Kalenderauswahl", en: "Calendar Selection",
fr: "Sélection du calendrier", it: "Selezione calendario") is a named bundle that gathers the
regions whose holidays should apply together.

**Important: regions do not inherit from their country.** A bundle is a plain list of
country-and-region pairs, and every pair it should include has to be listed. A bundle for one
canton that lists only that canton yields **no national holidays at all** — the national entry has
to be listed alongside it. The seeded bundles all do exactly that: each carries its national pair
plus its regional one.

<!-- level:elements -->

## Stage 2 — Writing a holiday date

Card anchor: `settings-calendar-rules`. The bundles sit at `settings-calendar-selection`.

**Fixed date** — `MM/DD`. New Year's Day is `01/01`.

**Fixed date pulled onto a weekday** — `MM/DD` followed by an offset and a weekday. The date is
first moved onto that weekday, and only then the offset is added. `09/01+14+SU` therefore means:
from 1 September go to the next Sunday, then add fourteen days — the third Sunday in September.

**Counted from Easter** — `EASTER` plus or minus a number of days. Ascension Day is `EASTER+39`,
Good Friday `EASTER-2`. Easter itself is computed, so these move correctly every year.

**The shift rule** (de: "Subregel") applies only when the computed date lands on a named weekday.
`SA-1;SU+1` means: if it falls on a Saturday move it one day back, if on a Sunday one day forward.
Only the first matching clause is applied.

**Official or not** (de: "Ist ein offizieller Feiertag") decides whether the day counts. There is
also a paid marker on the rule, but nothing evaluates it today — it is informational.

## The bundle and its entries

Each entry in a bundle is a country-and-region pair with one extra choice:
**reminder only** (de: "Nur als Erinnerung", en: "Reminder only"). A holiday marked that way is
still displayed but does not count towards pay. The hint on the checkbox says exactly this.

This override belongs to the bundle, not to the holiday rule — the same holiday can count in one
bundle and be a mere reminder in another. The underlying rule is never modified.

Bundles that ship with the system are marked (de: "System") and cannot be deleted, and neither can
a bundle that is currently in use.

<!-- level:effects -->

## Stage 3 — Where a bundle takes effect

A bundle can be attached in three places: as the company-wide default, on a group, and on a set of
working conditions. It cannot be attached to a person directly.

**The display and the payroll side do not read the same one.** The calendar shown while planning
follows the selected **group** and otherwise the company default. Payroll and macro calculations
follow the **working conditions** and otherwise the company default — the group is not consulted
there at all.

Both can therefore disagree: a day can appear red in the roster and still not count as a holiday
for pay, or the reverse. If somebody reports exactly that, this is where to look.

A third, older fallback exists for payroll when neither is set: a plain country-and-region setting.
That path matches the pair exactly and adds **no** national holidays and honours no reminder-only
choice — a company-wide region of "BE" yields only that region's holidays.

## Related skills

- `list_calendar_rules`, `create_calendar_rule`, `update_calendar_rule`, `delete_calendar_rule`
- `list_calendar_selections`, `create_calendar_selection`, `update_calendar_selection`, `delete_calendar_selection`
- `list_holidays_for_period`, `validate_holiday_overlap`

## Trigger phrases

- "Why is Whit Monday not showing as a holiday?"
- "Wie trage ich Ostern ein?"
- "Der Feiertag wird angezeigt, aber nicht bezahlt — warum?"
- "Do I have to add the national holidays separately?"
- "What happens when a holiday falls on a Sunday?"
