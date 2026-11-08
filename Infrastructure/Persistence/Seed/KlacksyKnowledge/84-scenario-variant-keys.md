---
name: explain_scenario_variant_keys
---

# Provisional shifts: shifts that only exist in scenarios

Provisional shifts (de: "Vorläufige Dienste", en: "Provisional shifts", fr: "Services provisoires", it: "Turni provvisori"; formerly called "keys") are a named set of non-binding shifts for a group and a period; they are visible only in scenarios that carry them and become real shifts when such a scenario is accepted. A single set is called only by its name, for example "Fair".

<!-- level:short -->

## What provisional shifts are for

Sometimes a large order is announced but its details are still open: how many people, which days, which times. Instead of creating real shifts too early, the supervisor puts candidate shifts into a set of provisional shifts, one set per variant. Each variant can then be planned in its own scenario, and the scenario that fits is accepted. Example: a customer announces an event with either two or three teams; the supervisor creates the sets "two teams" and "three teams", a scenario is planned with each, and the one the customer confirms is accepted.

<!-- level:elements -->

## Working with provisional shifts, step by step

1. **Create a set.** In the toolbar of the schedule, right after the hours recalculation, open the menu ⋮ (de: "Weitere Aktionen", en: "More actions", fr: "Plus d'actions", it: "Altre azioni") and choose (de: "Vorläufige Dienste verwalten", en: "Manage provisional shifts", fr: "Gérer les services provisoires", it: "Gestisci i turni provvisori"). The menu and this entry appear only for a Supervisor or an Admin (the right to edit shifts). The dialog (de: "Vorläufige Dienste", en: "Provisional shifts", fr: "Services provisoires", it: "Turni provvisori") lists the sets of the selected group and its subgroups. While no single group is selected, the entry is disabled with the hint (de: "Wähle zuerst eine Gruppe", en: "Select a group first"). Use (de: "Vorläufige Dienste anlegen", en: "Create provisional shifts", fr: "Créer des services provisoires", it: "Crea turni provvisori"); a set needs a name, a group and a period. The group cannot be changed afterwards.
2. **Add shifts.** Either create them with (de: "Neuer Dienst", en: "New shift", fr: "Nouveau service", it: "Nuovo turno") or change them with (de: "Dienste bearbeiten", en: "Edit shifts", fr: "Modifier les services", it: "Modifica turni"): the shift form then shows the set's name in a bar at the top, and everything saved there belongs to the set. Or copy existing sealed orders with (de: "Dienste kopieren", en: "Copy shifts", fr: "Copier les services", it: "Copia turni"). Every provisional shift needs a customer and a group inside the set's group, and must lie completely inside the set's period.
3. **Plan a variant.** Create a new scenario and tick the sets in the section (de: "Vorläufige Dienste für dieses Szenario", en: "Provisional shifts for this scenario", fr: "Services provisoires pour ce scénario", it: "Turni provvisori per questo scenario"). Several sets can be combined. This works for every planner, also without a role, but only for a scenario with a group, and only sets of that group or its subgroups are offered.
4. **Accept the fitting scenario.** The provisional shifts become real, sealed orders, and the set is marked (de: "Übernommen", en: "Accepted", fr: "Adoptés", it: "Accettati").

<!-- level:effects -->

## Rules around provisional shifts

### Visibility

Provisional shifts never appear in the real plan. They appear only in their management dialog and in scenarios that carry them. Everybody who sees the set's group can select it when creating a scenario; the management dialog is reached only through the menu ⋮ of a Supervisor or an Admin. There is no separate confidentiality flag; a confidential order goes into a group the people concerned do not see.

### Locked while in use

While an active scenario carries a set, the set and its shifts can be read but not changed, cut or deleted; otherwise the copy in the scenario would silently differ from the set.

### Accepting

Accepting a scenario with provisional shifts needs the right to edit shifts (Supervisor or Admin); creating, changing and deleting provisional shifts needs the same levels. Open slots of provisional shifts do not block the acceptance, they are shown as a warning. After the acceptance the set is used up: it cannot be changed or selected again, and other scenarios that carry the same set can only be discarded.

### Copies are templates

A copied shift is a template, not a replacement of the original. After the acceptance both the original order and the copy exist; the acceptance dialog and the scenario summary warn about this double demand.

### Discarding and deleting

Discarding a scenario keeps the set and its shifts for the next attempt. A set is deleted only by hand, and not while an active scenario carries it. A set whose period lies in the past stays selectable; the selection only shows a hint. An accepted set can no longer be deleted.

### Provisional shifts and placeholders

Provisional shifts handle uncertain shifts. When the people are missing as well, placeholders (`explain_placeholder_employees`) take the provisional shifts in the scenario: automatic planning puts real people first and leaves the rest to the placeholders. Placeholders are created in the same menu ⋮ of the schedule toolbar. After the acceptance, services on placeholders are part of the real plan; they block the period close and the payroll export until each placeholder is converted into a person or its services are moved to one.

### Related skills

Klacksy has no skills for creating or managing provisional shifts. It can read, plan in and discard a scenario that carries them like any other scenario; accepting it needs the right to edit shifts.

- `list_scenarios`, `accept_scenario`, `reject_scenario`

### Trigger phrases

- "Wie plane ich einen Auftrag, dessen Details noch offen sind?"
- "What are provisional shifts?"
- "Wo verwalte ich die vorläufigen Dienste?"
- "Warum kann ich die vorläufigen Dienste nicht mehr ändern?"
- "Why does the acceptance warn about double demand?"

<!-- level:pitfalls -->

## Consequences and edge cases

**Do not import the same order again.** An order accepted through provisional shifts is a real order afterwards. Importing it later from the ERP creates a second real order.

**A set is used once.** After one scenario with the set was accepted, the set cannot be chosen again; a new variant needs a new set.

**Shifts without a customer are not copied.** Orders without a customer, open orders that are not sealed yet, and shifts outside the set's group cannot be copied: copying a whole group skips and counts them, copying selected shifts refuses the whole selection. Repeating a copy creates the shifts a second time.

**Older wording.** Earlier versions called provisional shifts "keys", and their menu entry sat in the scenario selection. Both now live under the names above.
