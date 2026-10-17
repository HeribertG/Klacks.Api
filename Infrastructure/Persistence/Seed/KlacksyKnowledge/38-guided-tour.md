---
name: explain_guided_tour
description: |
  Explains the guided setup tour for administrators: twenty stations across the whole application
  where Klacksy explains each area, navigates to the page itself and makes the matching navigation
  icon pulse, while the user sets the pace and can skip, stop or ask questions at any station. It
  shows where things are rather than filling anything in, and can be repeated. Use this when
  somebody is new, asks where to begin, or wants an overview of the application.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  de:
    - tour
    - rundgang
    - neu hier
  en:
    - tour
    - walkthrough
    - new here
synonyms:
  de: [tour, rundgang, einführung, zeig mir die app, neu hier, überblick über die anwendung, einarbeitung]
  en: [tour, walkthrough, introduction, show me the app, new here, overview of the application, app tour]
  fr: [visite guidée, introduction, montre-moi l application, nouveau ici]
  it: [tour guidato, introduzione, mostrami l applicazione, nuovo qui]
---

# The guided setup tour

## Core idea (one sentence)

Rather than reading a manual, a new user is walked through the application station by station — and
Klacksy does the navigating.

## What happens

The tour is started from the chat, simply by asking for it. It is the **setup tour for the
administrator** of a new installation; its progress is stored for the whole installation and only an
administrator can read or continue it. From then on it goes through **twenty stations**: company name,
branding and company address, default language, the AI provider with its model sync and model check,
the planning optimiser, the Klacksy settings, the calendar (public holidays), email setup, absence
types, weekend and scheduling defaults, people, shifts, availability, period closing, user
administration, web search and plugins.

At each station three things happen together:

- Klacksy **explains the area** in a few sentences.
- It **navigates to the page** itself, so the explanation is never abstract.
- The matching **icon in the side navigation pulses**, so it is always obvious where in the
  application the current station sits.

## The user sets the pace

Nothing runs on a timer. Carry on to the next station, skip one, or end the tour — all at any point.
And at every station the tour can be interrupted with a question; Klacksy answers it in the context
of the page currently open, then picks up where it left off.

## Worth knowing

The tour can be repeated as often as wanted, which makes it useful beyond the first day: a new
colleague joining months later gets the same walkthrough without anybody having to prepare it.

It is an **explanation**, not a setup wizard: it shows where everything is and what it is for, but
it does not fill anything in. Actual configuration happens afterwards, in each area — or by asking
Klacksy to do it.

## Related skills

- `start_guided_tour` — begins the tour
- `navigate_to` — how Klacksy reaches each station, also usable on its own

## Trigger phrases

- "I'm new here, where do I start?"
- "Show me the application."
- "Can we go through the setup together?"
- "Is there an introduction for a new colleague?"
- "Mach eine Tour durch die App."
