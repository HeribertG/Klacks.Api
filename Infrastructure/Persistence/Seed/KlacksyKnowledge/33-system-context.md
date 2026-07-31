---
name: explain_system_context
description: |
  Explains what Klacksy knows about its surroundings and which of that reaches outside the company:
  the current date and time in the user's own timezone, the installed version and environment,
  weather from a free service that needs no key, and internet search which only works if it has been
  configured. Also covers the test-data generator, which despite its harmless name creates real
  records. Use this when the user asks whether Klacksy goes online, what leaves the company, what
  today's date is, or how to get practice data.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - internet
  - online
  - websuche
  - web search
  - wetter
  - testdaten
  - version
synonyms:
  de: [internet, online, websuche, geht ins internet, verlässt das haus, wetter, testdaten, testmitarbeiter, welche version, uhrzeit, datum]
  en: [internet, online, web search, goes online, leaves the company, weather, test data, which version, current time, date]
  fr: [internet, recherche web, en ligne, météo, données de test, quelle version, heure actuelle]
  it: [internet, ricerca web, online, meteo, dati di prova, quale versione, ora attuale]
---

# What Klacksy knows about its surroundings

## Core idea (one sentence)

Klacksy has a handful of abilities that look outward — and it matters which of them actually send
something out of the company.

## Time and date

Klacksy knows the current date and time **in the signed-in user's timezone**, not the server's. That
matters more than it sounds: "next week", "tomorrow" and "at the end of the month" all resolve
against this, so a wrong timezone quietly shifts planning dates.

## Version and environment

The installed version and which environment it runs in can be reported — useful when clarifying
whether a described behaviour matches the installed release.

## Weather

Weather and a short forecast come from a **free service that needs no key and no configuration**.
Without a place given, the company's own location is used. Only the location is sent — no personal
data is involved.

## Internet search

Search reaches the open internet and is the one ability here that genuinely leaves the company. Two
things follow:

- It **only works if it has been set up**. Without configuration there is no search, and Klacksy
  says so rather than inventing an answer.
- It is meant for things that must be current — mail server settings, recent events — **not** for
  general knowledge the model already has, and never as a detour around a skill that answers the
  question from the company's own data.

For an installation running on a local model, this is the deciding detail: without a configured
search, no request goes outside at all.

## Test data — not as harmless as it sounds

The test-data generator creates a **complete practice setup in one go**: several people with
realistic addresses, a group, a shared contract, and optionally round-the-clock shifts. These are
**real records in the real database**, not a sandbox. Useful for trying out automatic planning, but
worth doing deliberately and cleaning up afterwards.

## Related skills

- `get_current_time` — date and time in the user's timezone
- `get_system_info` — version and environment
- `get_weather` — weather without a key, company location by default
- `web_search` — the open internet, only when configured
- `create_test_environment` — practice data, creates real records

## Trigger phrases

- "Do you go online?"
- "What leaves the company?"
- "What is today's date?"
- "Create some test employees for me."
- "Welche Version läuft hier?"
