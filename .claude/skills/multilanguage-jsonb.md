# MultiLanguage JSONB Migration

## Konzept

MultiLanguage-Properties von separaten Spalten zu einer JSONB-Spalte konvertieren.

## Vorteile von JSONB

1. **Wartbarkeit**: Neue Sprachen nur in `MultiLanguage`-Klasse
2. **Performance**: Für Read-Heavy gleich schnell
3. **SQL-Kompatibilität**: Direkte JSONB-Rückgabe

## Nachteile

1. WHERE auf einzelne Sprachen weniger effizient
2. Einzelne Spalten nicht direkt indexierbar

## Migration

### 1. DataBaseContext anpassen

**Vorher:**
```csharp
entity.OwnsOne(b => b.Description, nav =>
{
    nav.Property(ml => ml.De).HasColumnName("description_de");
    nav.Property(ml => ml.En).HasColumnName("description_en");
    nav.Property(ml => ml.Fr).HasColumnName("description_fr");
    nav.Property(ml => ml.It).HasColumnName("description_it");
});
```

**Nachher:**
```csharp
entity.OwnsOne(b => b.Description, nav =>
{
    nav.ToJson("description");
});
```

### 2. Migration erstellen

```bash
dotnet ef migrations add Convert<Entity><Property>ToJsonb
```

### 3. Migration anpassen

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // JSONB-Spalte hinzufügen
    migrationBuilder.AddColumn<string>(
        name: "description",
        table: "break",
        type: "jsonb",
        nullable: true);

    // Daten migrieren
    migrationBuilder.Sql(@"
        UPDATE break
        SET description = jsonb_build_object(
            'De', description_de,
            'En', description_en,
            'Fr', description_fr,
            'It', description_it
        )
        WHERE description_de IS NOT NULL
           OR description_en IS NOT NULL;
    ");

    // Alte Spalten löschen
    migrationBuilder.DropColumn(name: "description_de", table: "break");
    migrationBuilder.DropColumn(name: "description_en", table: "break");
    migrationBuilder.DropColumn(name: "description_fr", table: "break");
    migrationBuilder.DropColumn(name: "description_it", table: "break");
}
```

### 4. SQL-Funktionen aktualisieren

**Vorher:**
```sql
jsonb_build_object(
    'De', b.description_de,
    'En', b.description_en
) AS description
```

**Nachher:**
```sql
b.description
```

## Entities mit MultiLanguage

| Entity | Property | Speicherung |
|--------|----------|-------------|
| Absence | Name | Separate Spalten |
| Absence | Description | Separate Spalten |
| Break | Description | **JSONB** |
| CalendarRule | Name | Separate Spalten |
| Countries | Name | Separate Spalten |
| Macro | Description | Separate Spalten |
| State | Name | Separate Spalten |

## Checkliste

- [ ] DataBaseContext: `OwnsOne` mit `ToJson()` konfigurieren
- [ ] Migration erstellen
- [ ] Migration mit Daten-Migration SQL anpassen
- [ ] SQL-Funktionen aktualisieren
- [ ] Mapper prüfen
- [ ] Migration anwenden
- [ ] Model-Dokumentation hinzufügen
- [ ] Testen
