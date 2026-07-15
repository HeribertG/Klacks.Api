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

In `docker-compose.yml`, uncomment the prepared lines on the `klacks-api`
service and place your profile as `./setup/region-setup.json` next to the
compose file (e.g. copy `regions/de.json` there):

```yaml
environment:
  - RegionSetup__File=/app/setup/region-setup.json
volumes:
  - ./setup:/app/setup:ro
```

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
exclusive entry shapes: a fixed-period cap (`period` Month/Quarter/Year +
`scope` + `capHours`) or a K6 rolling average (`windowWeeks` +
`maxAverageWeeklyHours`, e.g. 24 weeks / 48 h for the German ArbZG average or
17 weeks / 48 h for the UK WTR). Cap rows are imported as entities keyed by
`ImportSourceKey`; re-running the setup is idempotent.

## Industry profiles (K20 entity import)

The top-level `industryProfiles` map ships named per-industry presets. Each
block (keyed by an industry slug such as `healthcare`, `spitex`, `security`)
can carry `schedulingRulePresets` — named `SchedulingRule` rows whose fields
map 1:1 to the rule columns — and a `qualificationCatalog` — `Qualification`
rows with core-language names (`de`/`en`/`fr`/`it`) and an optional
`isTimeLimited` flag; the industry slug determines the qualification category.

All blocks are imported on every startup (never gated by a section marker):
each row carries a natural import key derived from the industry slug and the
preset/qualification name, re-runs reconcile changed file values, and a row
the customer has edited since the last import is never overwritten. Renaming
a preset in the file therefore creates a NEW row and leaves the old one
behind. Imported presets are selectable configuration — they change nothing
until a contract references the scheduling rule or a shift requires the
qualification.

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
