---
name: explain_llm_pseudonymization
---

# Pseudonymisation before a request reaches a cloud language model

Before a request goes to a cloud language model, Klacks replaces personal data with neutral pseudonyms and puts the real values back into the answer. The database always keeps plain text.

<!-- level:short -->

## What it is

- It applies to cloud models: OpenAI, Anthropic, Google, Mistral, DeepSeek, Azure and any other server outside the installation.
- Klacks replaces names of known employees, customers and users, phone numbers, e-mail addresses, personnel numbers and birthdates (a birthdate becomes an age).
- A pseudonym looks like `MA-0017`: MA employee, KL customer, BE user, NAME a name that several people share, TEL phone, MAIL e-mail.
- It is **on by default in every installation**.
- Say "pseudonymised", never "anonymous", "anonymised" or "without personal data".

<!-- level:elements -->

## How it works

1. The mapping between pseudonym and real value stays in the installation; it never goes to the provider. People keep the same number in every request (table `llm_pseudonyms`, which holds only internal ids, never a name or a value). Phone numbers and e-mail addresses are numbered within each request and never stored.
2. Known names are also replaced inside free text such as notes and mails. Other free text goes along unchanged.
3. In the answer Klacks puts the real values back, and again before any action runs, so the action always works on the real record.
4. Models inside the installation are not pseudonymised, because nothing leaves the house: an OpenAI-compatible provider (Ollama, LM Studio, a custom server) whose server address is localhost, a private network address, `host.docker.internal`, `*.local` or a single-word host name. The named cloud providers (OpenAI, Anthropic, Google, Mistral, DeepSeek, Azure) are always pseudonymised, whatever address they carry. A model name ending in "cloud" (Ollama cloud models) counts as cloud.
5. Only an administrator may turn it off, with the setting `LLM_PSEUDONYMIZATION_ENABLED` set to false. There is **no switch in the settings yet**; a later package adds one with a badge and a confirmation. Do not point the user to a button that does not exist.

<!-- level:effects -->

## Limits to state honestly

- **Notes are the user's responsibility, not Klacks'.** Klacks is for planning only, and notes are meant for planning matters only: health or medical details, AHV numbers and bank details do not belong in them; Klacks has no fields for them and does not look for them. Apart from known names and the values listed above, a note goes along as written.
- **Not covered:** unknown names (relatives, doctors, external people in mails), typos, names shorter than 3 letters, street addresses (planned for later), images. A request with an image is refused while pseudonymisation is active.
- **Voice output** with a cloud provider (default Microsoft Edge TTS, also OpenAI, ElevenLabs, Google) receives the restored answer with real names; cloud and browser speech recognition receive the audio. The **local voice** (speech output "local") and the local speech recognition keep text and audio inside the installation. They are an additional offer for the admin to choose: nothing switches automatically, and the speech settings show a hint while pseudonymisation is on and a cloud service is chosen. See `explain_klacksy_speech_setup`.
- **MCP and personal access token clients** receive tool results directly, without pseudonyms.
- Pseudonymised data are still personal data under GDPR and the Swiss revDSG: a data processing agreement with the provider stays necessary.
- The model may answer slightly less well: it cannot infer anything from a name. A short gender legend keeps the grammar right.
- Fully local models avoid the whole question, see `explain_klacksy_llm_setup`.

## Trigger phrases

- "Werden Namen an die KI geschickt?"
- "Is personal data sent to OpenAI?"
- "Sind die Daten anonym?"
- "Peut-on désactiver la pseudonymisation ?"
