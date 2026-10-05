---
name: explain_calendar_model
description: |
  Explains how public holidays are defined and which ones apply: date notation (a fixed day such as
  01/01, a date pulled onto a weekday, dates counted from Easter), the weekend shift rule, named bundles
  of regions and why a regional bundle must list its national entries, reminder-only holidays, which
  calendar decides surcharge and holiday-work warning, and when working on a holiday earns the time
  surcharge.
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

# Holidays — how a date is defined, which ones count, and for what

<!-- level:short -->

## Stage 1 — Two things, two switches, three questions

**A holiday rule** (de: "Feiertagsregeln", en: "Holiday rules", fr: "Règles des jours fériés",
it: "Regole per le vacanze") describes *when* a holiday falls, for one country and one region. It
carries a name, the notation for the date, an optional shift when it lands awkwardly, and two
switches.

**A calendar selection** (de: "Kalenderauswahl", en: "Calendar Selection",
fr: "Sélection du calendrier", it: "Selezione calendario") is a named bundle that gathers the
regions whose holidays should apply together.

**Important: regions do not inherit from their country.** A bundle is a plain list of
country-and-region pairs, and every pair it should include has to be listed. A bundle for one
canton that lists only that canton yields **no national holidays at all** — the national entry has
to be listed alongside it. The seeded bundles all do exactly that.

The two switches on a rule:

| Switch | Decides |
|---|---|
| **Official** (de: "Ist ein offizieller Feiertag") | Working on the day raises the warning "work on a statutory holiday", and only an official day can earn the holiday time surcharge. |
| **Time surcharge when working** (de: "Zeitzuschlag bei Arbeit", en: "Time surcharge when working", fr: "Supplément de temps en cas de travail", it: "Supplemento di tempo in caso di lavoro") | Only for official days: whoever works on it receives the holiday time surcharge — a time credit, not a wage payment. |

| Day is … | Holiday-work warning | Holiday time surcharge when worked |
|---|---|---|
| official, marked for the time surcharge | yes | yes |
| official, not marked | yes | no |
| not official, or "reminder only" in the calendar | no | no |

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

## The bundle and its entries

Each entry in a bundle is a country-and-region pair with one extra choice:
**reminder only** (de: "Nur als Erinnerung", en: "Reminder only"). A holiday from such an entry is
still displayed, but it never counts as official in this bundle: no holiday-work warning and no
holiday time surcharge, whatever the rule says.

This choice belongs to the bundle, not to the holiday rule — the same holiday can count in one
bundle and be a mere reminder in another. The underlying rule is never modified.

Bundles that ship with the system are marked (de: "System") and cannot be deleted, and neither can
a bundle that is currently in use.

<!-- level:effects -->

## Stage 3 — Which bundle decides what

A bundle can be attached in three places: as the company-wide default, on a group, and on a set of
working conditions (contract). It cannot be attached to a person directly.

**Warning and time surcharge follow the contract.** For a person on a given day the contract valid
on *that* day decides; without its own bundle the company default applies, and without that the
company country-and-region setting. That last, older fallback matches the pair exactly: it adds
**no** national holidays and knows no reminder-only choice. The group is not consulted here.

**The roster follows the group.** Which days are coloured, and on which holidays the roster lists a
shift that is ordered for holidays or not, follows the bundle of the group being viewed — its own
bundle, not a parent's — and otherwise the company default. Every entry counts there, reminder-only
ones included. The same shift can therefore show up on a day in one group's view and not in
another's. Typing a shift abbreviation into a schedule cell on a day where the roster does not list
that shift creates a note, not a work.

Both can disagree: a day can be coloured in the roster and still earn no surcharge, or the reverse.
If somebody reports exactly that, this is where to look.

**The warning** "work on a statutory holiday" appears for every day a work touches — a night shift
that runs past midnight into the holiday counts — when that day is official in the contract
calendar. It is a warning by default; the compliance enforcement setting can turn it into a refusal.
An exemption suppresses it, either for everybody or only for people whose contract uses one
scheduling rule (care, security and similar operations).

**The time surcharge** needs the day to be official and marked for the time surcharge, and the
contract's holiday rate to be above zero. When only the highest uplift is paid, a higher night,
Saturday or Sunday rate wins over the holiday rate. Absences are judged differently: for them an
official holiday counts whether or not it is marked for the time surcharge (a vacation day on an
official holiday, for example, books no hours). New holiday rules start marked for the time
surcharge; existing rules keep what they have.

**After a change** to a rule or a bundle the new holidays apply at once to new calculations, but works
that were already saved are not recalculated automatically.

**Switzerland:** whoever works in several cantons gets one contract per location, each with the
bundle of that location, so the right cantonal holidays count.

## Related skills

- `list_calendars`, `create_calendar_selection`, `update_calendar_selection`, `delete_calendar_selection`
- `import_calendar_rules`, `validate_calendar_rule`, `list_holidays_for_period`, `validate_holiday_overlap`
- `diagnose_holiday_outcome`, `get_compliance_enforcement_settings`, `update_compliance_enforcement_settings`

## Trigger phrases

- "Why is Whit Monday not showing as a holiday?"
- "Wie trage ich Ostern ein?"
- "Der Feiertag wird angezeigt, aber es gibt keinen Zuschlag — warum?"
- "Do I have to add the national holidays separately?"
- "What happens when a holiday falls on a Sunday?"
