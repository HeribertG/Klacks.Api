# Region Setup Profiles

Pre-configures a fresh Klacks installation for a country/region on first boot:
language plugins to install, locale (country/state/time zone), global holiday
calendar, weekend/week-start configuration, working-time limits, surcharge
rates and payroll export settings.

## How it works

- The API reads the file configured via the `RegionSetup__File` environment
  variable at startup. If the variable is unset, nothing happens.
- The top-level field `version` is required and must match the schema
  version this binary understands (currently `1`). A missing or unknown
  version fails the startup fast, before anything is written.
- Every profile section (`languages`, `locale`, `calendar`, `worktime`,
  `surcharges`, `export`) has its own marker setting
  (`REGION_SETUP_APPLIED_<SECTION>`) and is applied **exactly once,
  independently of the other sections**. A section already marked as applied
  is skipped even if the file changes; a section that is still unmarked is
  applied on the next start. This means a future new profile section added
  to a later schema version is picked up automatically on an
  already-configured installation, without touching the sections that were
  already applied.
- The original whole-file marker `REGION_SETUP_APPLIED` (SHA-256 of the file
  content) is still written on every successful run for backward
  compatibility. On an installation that predates the per-section markers,
  its mere presence marks all six sections above as already applied without
  rewriting their settings — the individual markers are backfilled on the
  first start after the upgrade.
- Invalid content (unknown JSON properties, invalid time zone, unknown day
  names or language plugin codes, unresolvable calendar selection) fails the
  startup fast, before anything is written.

## How to mount

The `klacks-api` service always mounts this whole `regions/` directory
read-only into the container. Select a profile via the installer, which
writes `REGION_SETUP_FILE` into `.env`:

```bash
# Linux
REGION=de ./install.sh

# Windows
powershell -ExecutionPolicy Bypass -File .\install.ps1 -Region de
```

Omitting `-Region`/`REGION` leaves `REGION_SETUP_FILE` empty and no region
setup is applied — this is unchanged on re-runs unless you pass `-Region`
again. Setting it manually in `.env` (`REGION_SETUP_FILE=/app/regions/de.json`)
works the same way without re-running the installer.

## Default language

`languages.default` sets the default UI language of the installation (setting
`DEFAULT_LANGUAGE`, delivered to the frontend via `GET /api/config/languages`).
The value must be a core language (`de`, `en`, `fr`, `it`), a code listed in
`languages.install`, or an already discovered language plugin — anything else
fails the setup before any write. If the field is omitted, the API falls back
to `en`.

## Overtime tiers and surcharge stacking (K3/K4)

`surcharges.overtime` configures up to three overtime tiers (`Overtime1`–`3`)
with `basis` (`day` or `week`, default `day`), `rateMode` (`multiplier` or
`fixedPerHour`) and `tiers` (strictly ascending `afterHours` plus `rate`).

`surcharges.stackingMode` (`highestWins` or `additive`) does NOT change any
arithmetic directly — stacking is a structural property of the macro assigned
to each shift. Two standard macros are seeded: `AllShift` (highest wins) and
`AllShiftAdditive` (night, weekend and holiday portions stack, e.g. KR/VN/PL).
The setting only selects which of the two is auto-assigned to newly created
shifts; planners can still pick the other macro per shift, so mixed operation
within one installation is supported.

## Compliance rules (enforcement, period caps, rolling averages)

`compliance.enforcement` sets warn/block per rule kind (`defaultMode` plus
per-rule overrides such as `rules.rollingAverage`) and
`allowSupervisorOverride`. `compliance.periodCaps` accepts two mutually
exclusive entry shapes: a fixed-period cap (`period` `month`/`quarter`/`year`/
`customWeeks` + `scope` `totalHours`|`overtimeHours` + `capHours`) or a K6
rolling average (`windowWeeks` + `maxAverageWeeklyHours`, e.g. 24 weeks / 48 h
for the German ArbZG average or 17 weeks / 48 h for the UK WTR). `scope`
selects what the cap counts: all worked hours (`totalHours`) or only overtime
hours beyond the contractual target (`overtimeHours`, e.g. a statutory annual
overtime ceiling such as 200 h/year). `period` `customWeeks` caps hours over a
trailing window of `customPeriodWeeks` weeks (1–104) ending on the evaluated
day instead of a calendar-anchored period; `customPeriodWeeks` is required for
(and only allowed with) that period value, and several `customWeeks` entries
with different window lengths may coexist (e.g. a 4-week and a 52-week
overtime cap, NO). `compliance.restDayRotations` adds rest-day
rotation rules (K10): at least `minFree` occurrences of `dayOfWeek` must stay
work-free within any trailing window of `windowWeeks` occurrences — e.g. 15
free Sundays in 52 weeks (German ArbZG §11) or 2 free Sundays in 4 weeks
("every 2nd Sunday free", CH). A vacation/sickness day counts as free; a
cross-midnight shift starting the evening before occupies the rest day. Cap
and rotation rows are imported as entities keyed by `ImportSourceKey`;
re-running the setup is idempotent and never overwrites customer-edited rows.

`compliance.counterRules` adds generic per-person event counters (K18):
`{ "event": "nightShift|workedDayInWeek|shiftExceedingHours", "period":
"week|month|year", "threshold": …, "hoursThreshold": … }` — e.g. warn from
the 25th night shift per year (CH) or on the 6th worked day per week (GR).
`hoursThreshold` is required for (and only allowed with) shiftExceedingHours.
Night-shift counting uses the K2 night window (an industry-scoped rule
prefers its bound rule's window). This stage warns or blocks (counterRule
enforcement mode); a surcharge-applying action is a later stage. The section
is also available per industry block (bound to the block's rule preset).

## Industry profiles (K20 entity import)

The top-level `industryProfiles` map ships named per-industry presets. Each
block (keyed by an industry slug such as `healthcare`, `homecare`, `security`
— see "Canonical industry slugs" below) can carry `schedulingRulePresets` —
named `SchedulingRule` rows whose fields map 1:1 to the rule columns — and a
`qualificationCatalog` — `Qualification` rows with core-language names
(`de`/`en`/`fr`/`it`) and an optional `isTimeLimited` flag; the industry slug
determines the qualification category.

### Canonical industry slugs

`industryProfiles` keys are technically free-form strings — the importer
accepts any slug. In practice, all shipped content (region profiles, future
country packs) is written against five canonical slugs so that country
profiles stay comparable and reusable across the product line. They line up
1:1 with the five industries the marketing site publishes per country
(`Klacks.Marketing/Localization/CountryIndustries.cs`), which uses
Swiss-German-flavoured slugs for its own routing:

| Setup slug (`industryProfiles.<slug>`) | Marketing slug | Industry |
| --- | --- | --- |
| `homecare` | `spitex` | Home care / ambulatory nursing services |
| `healthcare` | `spitaeler` | Hospitals / clinics (inpatient care) |
| `security` | `security` | Security services (guarding, surveillance) |
| `facility` | `hausdienste` | Facility services (cleaning, building services) |
| `logistics` | `logistik` | Logistics (warehousing, transport) |

The example profile `de.json` uses `healthcare` for its hospital/clinic
preset. `spitex`/`spitaeler`/`hausdienste`/`logistik` are the marketing site's
routing slugs only — do not use them as `industryProfiles` keys; use the
setup-side column above instead.

A block can additionally carry industry-SCOPED compliance rules: `periodCaps`
and `restDayRotations` (same entry shapes as the `compliance` sections) are
bound to the block's rule preset and then apply only to clients whose active
contract references that scheduling rule — a block carrying them must contain
exactly ONE `schedulingRulePresets` entry. A preset can also define its own
overtime ladder (`overtime` with `basis`, `rateMode` and up to 3 `tiers`); a
complete tier 1 on the rule overrides the global `surcharges.overtime`
settings entirely for that industry.

### Dated surcharge rates (`rateRevisions`)

A preset can carry dated `rateRevisions` — the surcharge rates AND the overtime
tier ladder that take effect from a given date onward, evaluated per work date
so a recomputation of past periods stays correct. Each entry needs a `validFrom`
(`yyyy-MM-dd`, strictly ascending within a preset) and at least one of
`nightRate`, `holidayRate`, `we1Rate`, `we2Rate`, `we3Rate`, or an `overtime`
block (same shape as the preset's `overtime`: `basis` `day`|`week`, `rateMode`
`multiplier`|`fixedPerHour`, up to 3 strictly-ascending `tiers`):

```jsonc
"industryProfiles": {
  "security": {
    "schedulingRulePresets": [
      {
        "name": "NO Vekter Standard",
        "maxWeeklyHours": 48,
        "nightRate": 0.22, "holidayRate": 1.0,
        "overtime": {
          "basis": "week",
          "tiers": [ { "afterHours": 40, "rate": 0.25 } ]
        },
        "rateRevisions": [
          { "validFrom": "2027-03-01",
            "nightRate": 0.27, "holidayRate": 1.0,
            "overtime": {
              "basis": "week",
              "tiers": [ { "afterHours": 38, "rate": 0.30 } ]
            } }
        ]
      }
    ]
  }
}
```

Each revision is a FULL snapshot, not a delta: for a work date on or after a
revision's `validFrom`, the latest such revision replaces the preset's base
surcharge rates AND base overtime ladder entirely. A rate you omit in a revision
does NOT keep the base rule's value or inherit from an earlier revision — it
falls through to the contract/settings chain. The same rule applies to
`overtime`: if the applicable revision has no `overtime` block, the overtime
surcharge falls through to the global `surcharges.overtime` settings, NOT the
base rule's ladder. To keep an unchanged rate or the overtime ladder at a
revision date you must restate it (the `holidayRate: 1.0` and the `overtime`
block above are repeated deliberately). Revisions are backend/import-only today
(no CRUD UI) and, like the presets themselves, change nothing until a contract
references the scheduling rule. Note for maintainers: when the set of fields in a
revision's content hash is extended (as the `overtime` block was), the importer
must keep a legacy-hash fallback so rows written by the previous binary are not
misread as customer-edited (frozen) on the next import. The base rule row and its revisions are
edit-protected independently: editing one does not freeze re-import of the
other. Because the profile file rejects unknown fields, deploy a binary that
knows `rateRevisions` BEFORE mounting a file that uses it.

All blocks are imported on every startup (never gated by a section marker):
each row carries a natural import key derived from the industry slug and the
preset/qualification name, re-runs reconcile changed file values, and a row
the customer has edited since the last import is never overwritten. Renaming
a preset in the file therefore creates a NEW row and leaves the old one
behind. Imported presets are selectable configuration — they change nothing
until a contract references the scheduling rule or a shift requires the
qualification.

## Active industries (selection visibility)

The optional top-level list `activeIndustries` declares which industries of the
profile are "armed" for this installation:

```jsonc
"activeIndustries": [ "homecare", "healthcare" ]
```

It only controls VISIBILITY in selection lists (dropdowns offering scheduling
rule presets, qualifications etc.) — ALL `industryProfiles` blocks are still
imported, so switching an industry on later needs no re-import. Omitting the
field means all industries are active. An admin can change the arming later via
the settings checkboxes (setting `ACTIVE_INDUSTRIES`, a comma-separated list of
industry slugs).

Validation is fail-fast before any write: every entry must be a key of the
`industryProfiles` map of the same file, an `activeIndustries` list without an
`industryProfiles` map is rejected, and an EMPTY list is rejected too ("empty =
all" is deliberately not supported — omit the field instead). The section has
its own marker (`REGION_SETUP_APPLIED_INDUSTRIES`) and is applied exactly once;
like every section added after the original six, it is also picked up on the
next start of an installation that predates it.

## Package identity

Marketplace-delivered profiles carry their identity in the optional top-level
`package` block:

```jsonc
"package": { "country": "de", "version": "1.2.0" }
```

`country` (two-letter ISO code) and `version` (non-empty string) are both
required when the block is present. The values are written to the settings
`REGION_PACKAGE_COUNTRY` and `REGION_PACKAGE_VERSION` on EVERY startup — never
gated by a section marker — so mounting a newer package version updates the
recorded identity in place. The profiles shipped in this directory do not carry
the block; the marketplace sets it when a package is downloaded.

## Macros (entity import)

The top-level `macros` list ships or replaces calculation macros:
`{ "name": …, "content": "<DSL script>", "function": "custom|standard|standardAdditive",
"category": "shift|vacation|…" }` (function defaults to custom, category to
shift). Every script is compiled AND probe-executed with neutral inputs at
import time under a hard timeout — a script error, a runtime failure or a
parser hang fails the setup fast instead of freezing a later work save.
Importing a standard function demotes the current SEEDED (or unedited
imported) holder of that function to custom; a customer-created holder is
never displaced — the import fails and tells the operator to resolve the
conflict deliberately. Prefer DATA over scripts: country/industry differences
belong in rates, night windows and tiers; ship a macro only when the
calculation STRUCTURE genuinely differs.

## Demo data

The top-level field `seedDemoData` controls whether demo/training data
(~5000 fake clients plus shifts and contracts) is seeded on the first boot.
Set it to `true` only for evaluation installations; `false` or omitting the
field means no demo data. When a region setup file is configured, this field
is the only switch — the legacy `Fake__WithFake` configuration is ignored.

All profile blocks and fields are optional; only the provided values are
written. See `de.json` for a realistic German profile — adjust `locale.state`
and `locale.calendarSelection.state` to the customer's federal state before
mounting, because public holidays differ per state.
