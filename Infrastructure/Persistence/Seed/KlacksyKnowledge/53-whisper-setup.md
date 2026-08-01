---
name: explain_whisper_setup
description: |
  Explains running speech recognition on the company's own server instead of sending recordings to
  an outside service: the two model sizes and what they cost in memory, that installing downloads a
  large model and can take a good while, how progress is shown, switching between the sizes, and
  what happens to dictation when it is removed again. Use this when the user asks whether recordings
  can stay in-house, why an installation is taking so long, or which size to pick.
category: Query
executionType: Skill
alwaysOn: false
triggerKeywords:
  mul:
    - whisper
  de:
    - lokale spracherkennung
    - selbst gehostet
    - ohne cloud
  en:
    - self-hosted speech recognition
    - local speech recognition
synonyms:
  de: [whisper, lokale spracherkennung, selbst gehostete spracherkennung, spracherkennung ohne cloud, sprachdaten im haus, modell herunterladen, welches modell soll ich nehmen]
  en: [whisper, self-hosted speech recognition, local speech recognition, speech without cloud, keep recordings in house, download model, which model size]
  fr: [whisper, reconnaissance vocale locale, auto-hébergée, sans cloud, télécharger le modèle]
  it: [whisper, riconoscimento vocale locale, self-hosted, senza cloud, scaricare il modello]
---

# Self-hosted speech recognition

## Core idea (one sentence)

Installs a speech service on the company's own server so that recordings never leave the building —
the deciding argument wherever staff data must not reach an outside provider.

## The two sizes

Card anchor: `whisper-plugin-setting-container`.

- **Compact** (de: "Kompakt (small)") — less memory, weaker on uncommon languages.
- **Full** (de: "Voll (large-v3-turbo)") — all 25 languages, around 2 GB of memory. This is the
  preselected choice.

Those are the only differences the product states. Anything more specific about accuracy or speed
would be guesswork.

## Installing

Pick a size and install. **Expect it to take a while** — the model has to be downloaded, and the
card warns this can run up to roughly 25 minutes.

While it runs, the card polls every few seconds and shows the state: pending, running, succeeded or
failed. Buttons stay locked meanwhile.

**Only one such operation at a time.** A running application update blocks installation, and the
card says so plainly. The reverse holds too: installing here blocks an application update until it
finishes.

## Switching size

Install the other size — that is the whole procedure. The switch button appears only once something
is installed and a different size is selected.

## Removing it

Removing puts speech recognition **back to the browser** before the service is stopped, so dictation
keeps working rather than breaking the moment the service disappears.

## Related skills

- `get_whisper_plugin_status`, `install_whisper_plugin`, `uninstall_whisper_plugin`

## Trigger phrases

- "Können wir die Spracherkennung im Haus behalten?"
- "Welches Modell soll ich nehmen?"
- "Die Installation läuft seit zehn Minuten."
- "Warum ist der Installieren-Knopf gesperrt?"
- "Was passiert mit dem Diktat, wenn ich das entferne?"
