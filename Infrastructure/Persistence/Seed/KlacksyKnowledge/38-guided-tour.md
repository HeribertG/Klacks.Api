---
name: explain_guided_tour
description: |
  Explains the guided setup tour: Klacksy walks a new user through sixteen stations covering the
  whole application, navigating to each page itself and making the matching navigation icon pulse.
  Covers how to start it, that the user sets the pace and can skip or stop at any point, that
  questions can be asked at every station, and that it can be repeated for new colleagues. Use this
  when somebody is new, asks where to begin, or wants an overview of the application.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  - tour
  - einführung
  - neu hier
  - wo fange ich an
  - rundgang
synonyms:
  de: [tour, rundgang, einführung, zeig mir die app, neu hier, wo fange ich an, erste schritte, überblick über die anwendung, einarbeitung]
  en: [tour, walkthrough, introduction, show me the app, new here, where do i start, first steps, overview of the application, onboarding]
  fr: [visite guidée, introduction, montre-moi l application, nouveau ici, par où commencer, premiers pas]
  it: [tour guidato, introduzione, mostrami l applicazione, nuovo qui, da dove comincio, primi passi]
---

# The guided setup tour

## Core idea (one sentence)

Rather than reading a manual, a new user is walked through the application station by station — and
Klacksy does the navigating.

## What happens

The tour is started from the chat, simply by asking for it. From then on it goes through **sixteen
stations**, covering the whole application: company basics and the company address, calendars, user
administration, groups, people, shifts, availability, absences, public holidays, period closing,
email setup, and the assistant and plugin settings.

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
