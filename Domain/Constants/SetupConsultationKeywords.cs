// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Word lists the setup consultation classifies its two free-text answers with. Domain nouns are
/// listed separately from plain yes/no because they must be checked FIRST: the affirmation tokens
/// include "bitte", "mach" and "gerne", so "Bitte intern" would otherwise be read as a yes.
/// <see cref="UnclearMarkers"/> exists because DeclineDetector treats "kein"/"keine"/"keinen" as a
/// leading negation, so an idiom like "Keine Ahnung" ("no idea") would otherwise be misread as a
/// concrete No — it must be checked before the negation fallback so it wins only when no domain
/// noun already answered the question.
/// Core languages only (de/en/fr/it); plugin languages degrade to yes/no and then to Unknown, which
/// is the safe path.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class SetupConsultationKeywords
{
    public static readonly HashSet<string> AttributedToCustomer = new(StringComparer.OrdinalIgnoreCase)
    {
        "kunde", "kunden", "kundin", "auftraggeber", "mandant", "verrechnet", "verrechnung",
        "rechnung", "client", "clients", "customer", "customers", "billed", "billing",
        "facture", "facturé", "facturation", "cliente", "clienti", "fattura", "fatturato",
    };

    public static readonly HashSet<string> AttributedToNobody = new(StringComparer.OrdinalIgnoreCase)
    {
        "intern", "interne", "internen", "eigenbetrieb", "station", "abteilung", "küche",
        "kueche", "werkstatt", "innendienst", "backoffice", "büro", "buero",
        "internal", "internally", "ward", "kitchen", "workshop", "inhouse",
        "interno", "cucina", "reparto", "officina",
    };

    public static readonly HashSet<string> OrderSourceExternal = new(StringComparer.OrdinalIgnoreCase)
    {
        "erp", "sap", "abacus", "navision", "dynamics", "schnittstelle", "fremdsystem",
        "import", "xml", "drittsystem", "vorsystem",
        "external", "interface", "externe", "esterno", "importazione",
    };

    public static readonly HashSet<string> OrderSourceManual = new(StringComparer.OrdinalIgnoreCase)
    {
        "manuell", "manuelle", "hand", "selber", "selbst", "eintippen", "erfassen",
        "manual", "manually", "manuellement", "manualmente",
    };

    public static readonly HashSet<string> NextStepCreate = new(StringComparer.OrdinalIgnoreCase)
    {
        "anlegen", "erstellen", "erfassen", "zusammen", "gemeinsam", "geführt", "gefuehrt",
        "create", "together", "guided", "créer", "ensemble", "creare", "insieme",
    };

    public static readonly HashSet<string> NextStepShow = new(StringComparer.OrdinalIgnoreCase)
    {
        "zeigen", "zeig", "wo", "ansehen", "hinführen", "hinfuehren", "navigier",
        "show", "where", "montre", "montrer", "où", "mostra", "dove",
    };

    public static readonly HashSet<string> UnclearMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "ahnung", "unklar", "unsicher",
        "idea", "clue", "unsure",
        "idée", "sûr",
        "sicuro",
    };

    public static readonly HashSet<string> NegationMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        "kein", "keine", "keinen", "keiner", "nicht",
        "no", "not", "without",
        "ohne", "pas", "aucun",
        "nessun", "senza", "nemmeno",
    };
}
