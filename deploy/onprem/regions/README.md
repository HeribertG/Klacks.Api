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
