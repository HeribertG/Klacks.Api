---
name: explain_klacksy_speech_setup
description: |
  Explains talking to the assistant instead of typing: choosing what turns speech into text and what
  reads answers aloud, whether answers are read automatically, how long a pause must be before an
  utterance counts as finished, and whether talking over an answer stops it. Also covers the glossary
  that teaches rare names and terms so they stop being transcribed wrongly. Use this when the user
  asks about dictation, having answers read out, or a word that keeps coming out garbled.
category: Query
executionType: Skill
alwaysOn: false
parameters:
  - name: level
    type: enum
    required: false
    enumValues: [short, elements, effects]
triggerKeywords:
  de:
    - spracheingabe
    - sprachausgabe
    - diktieren
    - vorlesen
    - sprechpause
  en:
    - speech recognition
    - text to speech
    - dictation
    - read aloud
synonyms:
  de: [spracheingabe, sprachausgabe, diktieren, vorlesen lassen, stimme, sprechpause, unterbrechen beim sprechen, wörterbuch für spracherkennung, name wird falsch verstanden, mikrofon]
  en: [speech recognition, text to speech, dictation, read answers aloud, voice, speech pause, interrupt while speaking, transcription dictionary, name transcribed wrongly]
  fr: [reconnaissance vocale, synthèse vocale, dicter, lire à voix haute, pause de parole, dictionnaire de transcription]
  it: [riconoscimento vocale, sintesi vocale, dettare, lettura ad alta voce, pausa vocale, dizionario di trascrizione]
---

# Speaking with the assistant

<!-- level:short -->

## Stage 1 — What this is for

Instead of typing, you can speak; instead of reading, you can listen. Two independent halves
(de: "Klacksy Sprache", en: "Klacksy Speech"):

- **Speech recognition** (de: "Spracherkennung") turns what you say into text. The browser can do
  this for free, or an external service can do it better.
- **Speech output** (de: "Sprachausgabe Anbieter") reads answers aloud. One option needs no account.

**The spoken language is not set here** — it follows the language of the interface. There is also no
speed control.

<!-- level:elements -->

## Stage 2 — The settings

Card anchor: `settings-assistant-speech`.

- **Speech recognition** (field `sttEngine`) and, where the chosen service needs one, its access key.
  A key already stored on the server is shown as placeholder dots rather than being displayed.
- **Speech output** (field `ttsProvider`) plus its key, and a **voice** (field `ttsVoice`). Left on
  automatic, a voice matching the interface language is chosen. The voices offered are simply the
  list the chosen service publishes; they are not ranked or scored.
- **Output mode** (field `outputMode`) — text only, text and audio, text and audio with answers read
  **automatically**, or audio only. The automatic setting is what people mean by "read it to me
  without my asking"; audio only reduces the assistant to a small icon.
- **Speech pause detection** (field `silenceThreshold`) — how long a silence must last before your
  utterance counts as finished. Between half a second and three seconds, one second by default.
  Shorter reacts faster, longer gives you room to think mid-sentence.
- **Text tidying** — a language model cleans up the raw transcript. There is a separate model choice
  for this and an editable instruction, which can be reset to its original.
- **Interruption** (de: "Sprach-Unterbrechung (Barge-in)") — see below. **Off by default.**

There is no save button; changes are stored on their own.

### Interruption while an answer is spoken

With this on, the microphone stays open while the assistant answers, and speaking over it stops the
playback. To avoid the assistant's own voice triggering it, the threshold is raised while listening,
and speech must persist for a good fraction of a second before the interruption is taken as meant —
a cough will not stop it. It works most reliably with a headset or with a service that recognises
speech continuously. If the microphone cannot be opened, the answer simply plays through.

<!-- level:effects -->

## Stage 3 — The glossary

The glossary (de: "Wörterbuch", en: "Dictionary") is where you teach the recognition the words it
keeps getting wrong: staff names, place names, in-house terms.

Each entry has:

- **Term** — the correct spelling.
- **Variants** — what it tends to come out as, separated by commas.
- **Language** — automatic, or restricted to one language. An entry left on automatic always applies;
  one bound to a language applies only when that language is spoken.

It works in three ways at once: the correct terms are handed to the recognition as a hint of what to
expect, the variants are replaced by the correct term afterwards, and the whole glossary is offered
to the language model that tidies the text. Replacement respects word boundaries and ignores
capitalisation; longer variants are tried before shorter ones.

Changes take effect within a few minutes, without restarting anything.

## Related skills

- `get_speech_settings`, `update_speech_settings`
- `list_dictionary_entries`, `create_dictionary_entry`, `update_dictionary_entry`, `delete_dictionary_entry`

## Trigger phrases

- "Kann ich Klacksy diktieren?"
- "Soll er die Antwort automatisch vorlesen?"
- "Er versteht den Nachnamen immer falsch."
- "Kann ich ihn unterbrechen, während er spricht?"
- "Er schickt ab, bevor ich zu Ende gesprochen habe."
