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

# The guided tours

## Core idea (one sentence)

Rather than reading a manual, a new user is walked through the application station by station — and
Klacksy does the navigating.

## Two tours

- The **short introduction** is there for **every user account**, whatever its role. It covers the
  everyday areas the person is allowed to see: the schedule (including the group selection and why
  not everybody is visible), shifts and orders, absences, people and addresses, period closing (only
  with that right), Klacksy itself (asking questions, asking for simpler or shorter answers, the fact
  that there are commands) and the inbox when the messaging plugin is active. Each station is two or
  three sentences; asking for more goes deeper into that page. Its progress belongs to the account.
- The **setup tour** is for the **administrator** of a new installation. Its progress is stored for
  the whole installation and only an administrator can read or continue it. It goes through
  **twenty stations**: company name, branding and company address, default language, the AI
  provider with its model sync and model check, the planning optimiser, the Klacksy settings, the
  calendar (public holidays), email setup, absence types, weekend and scheduling defaults, people,
  shifts, availability, period closing, user administration, web search and plugins. An
  administrator who finished or declined the setup tour is not offered the short introduction on
  top.

## How it is offered

A new account is **offered** the short introduction, never pushed into it: accepting, postponing
and declining are equally visible choices. Postponing moves the offer to the next session, at most
twice; after that, and after declining, it is not offered again. Accounts that already existed when
the introduction was added are offered it exactly once. After declining, Klacksy may offer one
follow-up for people who already know Klacks, which makes its explanations shorter from then on. Either tour can be started again at
any time simply by asking for it ("start the introduction again", "show me the setup tour").

## What happens at a station

At each station three things happen together:

- Klacksy **explains the area** in a few sentences.
- It **navigates to the page** itself, so the explanation is never abstract.
- The matching **icon in the side navigation pulses**, so it is always obvious where in the
  application the current station sits.

## The user sets the pace

Nothing runs on a timer. Carry on to the next station, skip one, go back, or end the tour — all at
any point. And at every station the tour can be interrupted with a question; Klacksy answers it in
the context of the page currently open, then picks up where it left off. An interrupted tour
resumes at the first station not yet done.

## Worth knowing

Both tours can be repeated as often as wanted, which makes them useful beyond the first day: a new
colleague joining months later gets the introduction without anybody having to prepare it.

They are **explanations**, not setup wizards: they show where everything is and what it is for, but
they do not fill anything in. Actual configuration happens afterwards, in each area — or by asking
Klacksy to do it.

## Related skills

- `start_guided_tour` — begins a tour (the short introduction by default, the setup tour on request)
- `navigate_to` — how Klacksy reaches each station, also usable on its own

## Trigger phrases

- "I'm new here, where do I start?"
- "Show me the application."
- "Start the introduction again."
- "Can we go through the setup together?"
- "Is there an introduction for a new colleague?"
- "Mach eine Tour durch die App."
