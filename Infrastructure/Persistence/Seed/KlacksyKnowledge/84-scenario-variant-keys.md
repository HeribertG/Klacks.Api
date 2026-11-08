---
name: explain_scenario_variant_keys
---

# Keys: shifts that only exist in scenarios

A key is a named set of non-binding shifts for a group and a period; the shifts are visible only in scenarios that carry the key and become real shifts when such a scenario is accepted.

<!-- level:short -->

## What keys are for

Sometimes a large order is announced but its details are still open: how many people, which days, which times. Instead of creating real shifts too early, the planner puts candidate shifts into a key, one key per variant. Each variant can then be planned in its own scenario, and the scenario that fits is accepted. Example: a customer announces an event with either two or three teams; the planner creates the keys "two teams" and "three teams", plans one scenario with each and accepts the one the customer confirms.

<!-- level:elements -->

## Working with keys, step by step

1. **Create a key.** In the schedule open the scenario selection and choose (de: "Keys verwalten", en: "Manage keys", fr: "Gérer les clés", it: "Gestisci le chiavi"). The dialog (de: "Keys für nicht scharfe Dienste", en: "Keys for non-binding shifts", fr: "Clés pour services non contraignants", it: "Chiavi per turni non vincolanti") lists the keys of the selected group and its subgroups. Use (de: "Neuen Key anlegen", en: "Create new key", fr: "Créer une nouvelle clé", it: "Crea nuova chiave"); a key needs a name, a group and a period. The group cannot be changed afterwards.
2. **Add shifts.** Either create them with (de: "Neuer Dienst", en: "New shift", fr: "Nouveau service", it: "Nuovo turno") or change them with (de: "Dienste bearbeiten", en: "Edit shifts", fr: "Modifier les services", it: "Modifica turni"): the shift form then shows the key name in a bar at the top, and everything saved there belongs to the key. Or copy existing sealed orders with (de: "Dienste kopieren", en: "Copy shifts", fr: "Copier les services", it: "Copia turni"). Every key shift needs a customer and a group inside the key's group, and must lie completely inside the key's period.
3. **Plan a variant.** Create a new scenario and tick the keys in the section (de: "Keys für dieses Szenario", en: "Keys for this scenario", fr: "Clés pour ce scénario", it: "Chiavi per questo scenario"). Several keys can be combined. This only works for a scenario with a group, and only keys of that group or its subgroups are offered.
4. **Accept the fitting scenario.** The key's shifts become real, sealed orders, and the key is marked (de: "Übernommen", en: "Accepted", fr: "Adoptée", it: "Accettata").

<!-- level:effects -->

## Rules around keys

### Visibility

Key shifts never appear in the real plan. They appear only in the key management and in scenarios that carry the key. Everybody who sees the key's group can read and select a key; there is no separate confidentiality flag. A confidential order goes into a group the people concerned do not see.

### Locked while in use

While an active scenario carries a key, the key and its shifts can be read but not changed, cut or deleted; otherwise the copy in the scenario would silently differ from the key.

### Accepting

Accepting a scenario with keys needs the right to edit shifts (Supervisor or Admin); creating, changing and deleting keys needs the same levels. Open slots of key shifts do not block the acceptance, they are shown as a warning. After the acceptance the key is used up: it cannot be changed or selected again, and other scenarios that carry the same key can only be discarded.

### Copies are templates

A copied shift is a template, not a replacement of the original. After the acceptance both the original order and the copy exist; the acceptance dialog and the scenario summary warn about this double demand.

### Discarding and deleting

Discarding a scenario keeps the key and its shifts for the next attempt. A key is deleted only by hand, and not while an active scenario carries it. A key whose period lies in the past stays selectable; the selection only shows a hint. An accepted key can no longer be deleted.

### Keys and placeholders

Keys handle uncertain shifts. When the people are missing as well, placeholders (`explain_placeholder_employees`) take the key shifts in the scenario: automatic planning puts real people first and leaves the rest to the placeholders. After the acceptance, services on placeholders are part of the real plan; they block the period close and the payroll export until each placeholder is converted into a person or its services are moved to one.

### Related skills

Klacksy has no skills for creating or managing keys. It can read, plan in and discard a scenario that carries keys like any other scenario; accepting it needs the right to edit shifts.

- `list_scenarios`, `accept_scenario`, `reject_scenario`

### Trigger phrases

- "Wie plane ich einen Auftrag, dessen Details noch offen sind?"
- "What is a key in the scenario menu?"
- "Warum kann ich den Key nicht mehr ändern?"
- "Why does the acceptance warn about double demand?"

<!-- level:pitfalls -->

## Consequences and edge cases

**Do not import the same order again.** An order accepted through a key is a real order afterwards. Importing it later from the ERP creates a second real order.

**A key is used once.** After one scenario with the key was accepted, the key cannot be chosen again; a new variant needs a new key.

**Shifts without a customer are not copied.** Orders without a customer, open orders that are not sealed yet, and shifts outside the key's group cannot be copied: copying a whole group skips and counts them, copying selected shifts refuses the whole selection. Repeating a copy creates the shifts a second time.
