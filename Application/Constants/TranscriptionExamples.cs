// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Locale-specific few-shot examples for the transcription enhancement prompt.
/// Covers the four core languages plus all installable language packs; falls back to English.
/// </summary>
/// <param name="locale">Locale code such as "de", "de-CH", "pt-BR" or "zh-CN"</param>
namespace Klacks.Api.Application.Constants;

public static class TranscriptionExamples
{
    private const string SectionHeader = "\n\nExamples:\n";
    private const string FallbackLanguage = "en";

    private static readonly IReadOnlyDictionary<string, string> ByLanguage =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] = """
                Input: ähm also erstelle mal einen einen neuen mitarbeiter der heisst äh hans müller
                Output: Erstelle einen neuen Mitarbeiter, der heisst Hans Müller.
                Input: verschiebe den dienst von dienstag äh nein mittwoch auf donnerstag
                Output: Verschiebe den Dienst von Mittwoch auf Donnerstag.
                Input: wie viele mitarbeiter arbeiten am montag
                Output: Wie viele Mitarbeiter arbeiten am Montag?
                """,
            ["en"] = """
                Input: um can you show me the the shift plan for uh next week
                Output: Can you show me the shift plan for next week?
                Input: move the shift from tuesday uh no wednesday to thursday
                Output: Move the shift from Wednesday to Thursday.
                Input: how many employees are working on monday
                Output: How many employees are working on Monday?
                """,
            ["fr"] = """
                Input: euh alors crée un un nouvel employé qui s'appelle ben jean dupont
                Output: Crée un nouvel employé qui s'appelle Jean Dupont.
                Input: combien d'employés travaillent lundi
                Output: Combien d'employés travaillent lundi ?
                """,
            ["it"] = """
                Input: ehm allora crea un un nuovo dipendente che si chiama cioè mario rossi
                Output: Crea un nuovo dipendente che si chiama Mario Rossi.
                Input: quanti dipendenti lavorano lunedì
                Output: Quanti dipendenti lavorano lunedì?
                """,
            ["es"] = """
                Input: este pues crea un un nuevo empleado que se llama eh juan garcía
                Output: Crea un nuevo empleado que se llama Juan García.
                Input: cuántos empleados trabajan el lunes
                Output: ¿Cuántos empleados trabajan el lunes?
                """,
            ["pt"] = """
                Input: é tipo cria um um novo funcionário que se chama hã joão silva
                Output: Cria um novo funcionário que se chama João Silva.
                Input: quantos funcionários trabalham na segunda-feira
                Output: Quantos funcionários trabalham na segunda-feira?
                """,
            ["nl"] = """
                Input: uhm maak eens een een nieuwe medewerker aan die heet eh jan de vries
                Output: Maak een nieuwe medewerker aan die heet Jan de Vries.
                Input: hoeveel medewerkers werken er op maandag
                Output: Hoeveel medewerkers werken er op maandag?
                """,
            ["pl"] = """
                Input: yyy no utwórz utwórz nowego pracownika który nazywa się eee jan kowalski
                Output: Utwórz nowego pracownika, który nazywa się Jan Kowalski.
                Input: ilu pracowników pracuje w poniedziałek
                Output: Ilu pracowników pracuje w poniedziałek?
                """,
            ["cs"] = """
                Input: ehm tak vytvoř vytvoř nového zaměstnance který se jmenuje eee jan novák
                Output: Vytvoř nového zaměstnance, který se jmenuje Jan Novák.
                Input: kolik zaměstnanců pracuje v pondělí
                Output: Kolik zaměstnanců pracuje v pondělí?
                """,
            ["ro"] = """
                Input: ăă deci creează un un angajat nou care se numește păi ion popescu
                Output: Creează un angajat nou care se numește Ion Popescu.
                Input: câți angajați lucrează luni
                Output: Câți angajați lucrează luni?
                """,
            ["el"] = """
                Input: εε λοιπόν δημιούργησε έναν έναν νέο υπάλληλο που λέγεται χμ γιώργος παπαδόπουλος
                Output: Δημιούργησε έναν νέο υπάλληλο που λέγεται Γιώργος Παπαδόπουλος.
                Input: πόσοι υπάλληλοι εργάζονται τη δευτέρα
                Output: Πόσοι υπάλληλοι εργάζονται τη Δευτέρα;
                """,
            ["sv"] = """
                Input: öhm skapa en en ny medarbetare som heter öh anna svensson
                Output: Skapa en ny medarbetare som heter Anna Svensson.
                Input: hur många medarbetare jobbar på måndag
                Output: Hur många medarbetare jobbar på måndag?
                """,
            ["da"] = """
                Input: øhm opret en en ny medarbejder der hedder øh anders jensen
                Output: Opret en ny medarbejder, der hedder Anders Jensen.
                Input: hvor mange medarbejdere arbejder på mandag
                Output: Hvor mange medarbejdere arbejder på mandag?
                """,
            ["nb"] = """
                Input: øhm opprett en en ny medarbeider som heter øh ola nordmann
                Output: Opprett en ny medarbeider som heter Ola Nordmann.
                Input: hvor mange medarbeidere jobber på mandag
                Output: Hvor mange medarbeidere jobber på mandag?
                """,
            ["fi"] = """
                Input: öö niinku luo uusi uusi työntekijä jonka nimi on tota matti virtanen
                Output: Luo uusi työntekijä, jonka nimi on Matti Virtanen.
                Input: kuinka monta työntekijää on töissä maanantaina
                Output: Kuinka monta työntekijää on töissä maanantaina?
                """,
            ["ar"] = """
                Input: يعني أنشئ أنشئ موظفاً جديداً اسمه اه أحمد خالد
                Output: أنشئ موظفاً جديداً اسمه أحمد خالد.
                Input: كم عدد الموظفين الذين يعملون يوم الاثنين
                Output: كم عدد الموظفين الذين يعملون يوم الاثنين؟
                """,
            ["he"] = """
                Input: אה כאילו תיצור תיצור עובד חדש שקוראים לו אמ דוד כהן
                Output: תיצור עובד חדש שקוראים לו דוד כהן.
                Input: כמה עובדים עובדים ביום שני
                Output: כמה עובדים עובדים ביום שני?
                """,
            ["id"] = """
                Input: eh anu buat buat karyawan baru namanya itu budi santoso
                Output: Buat karyawan baru namanya Budi Santoso.
                Input: berapa karyawan yang bekerja hari senin
                Output: Berapa karyawan yang bekerja hari Senin?
                """,
            ["ms"] = """
                Input: eh anu cipta cipta pekerja baru namanya itu ahmad bin ali
                Output: Cipta pekerja baru namanya Ahmad bin Ali.
                Input: berapa pekerja yang bekerja pada hari isnin
                Output: Berapa pekerja yang bekerja pada hari Isnin?
                """,
            ["vi"] = """
                Input: ừm à tạo tạo một nhân viên mới tên là kiểu nguyễn văn an
                Output: Tạo một nhân viên mới tên là Nguyễn Văn An.
                Input: có bao nhiêu nhân viên làm việc vào thứ hai
                Output: Có bao nhiêu nhân viên làm việc vào thứ Hai?
                """,
            ["th"] = """
                Input: เอ่อ สร้าง สร้างพนักงานใหม่ชื่อ แบบ สมชาย ใจดี
                Output: สร้างพนักงานใหม่ชื่อสมชาย ใจดี
                Input: มีพนักงานทำงานวันจันทร์กี่คน
                Output: มีพนักงานทำงานวันจันทร์กี่คน
                """,
            ["ja"] = """
                Input: えーと あのー 新しい従業員を 従業員を作成して 名前は えっと 田中太郎
                Output: 新しい従業員を作成して、名前は田中太郎。
                Input: 月曜日に何人の従業員が働いていますか
                Output: 月曜日に何人の従業員が働いていますか？
                """,
            ["ko"] = """
                Input: 음 그 새 직원을 직원을 만들어줘 이름은 어 김철수
                Output: 새 직원을 만들어줘. 이름은 김철수.
                Input: 월요일에 몇 명의 직원이 일하나요
                Output: 월요일에 몇 명의 직원이 일하나요?
                """,
            ["zh-CN"] = """
                Input: 嗯 那个 创建一个一个新员工 名字叫 就是 王伟
                Output: 创建一个新员工，名字叫王伟。
                Input: 星期一有多少员工上班
                Output: 星期一有多少员工上班？
                """,
            ["zh-TW"] = """
                Input: 嗯 那個 建立一個一個新員工 名字叫 就是 王偉
                Output: 建立一個新員工，名字叫王偉。
                Input: 星期一有多少員工上班
                Output: 星期一有多少員工上班？
                """,
        };

    public static string GetForLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return SectionHeader + ByLanguage[FallbackLanguage];
        }

        var trimmed = locale.Trim();
        if (ByLanguage.TryGetValue(trimmed, out var exact))
        {
            return SectionHeader + exact;
        }

        var separatorIndex = trimmed.IndexOfAny(['-', '_']);
        var baseLanguage = separatorIndex > 0 ? trimmed[..separatorIndex] : trimmed;
        if (ByLanguage.TryGetValue(baseLanguage, out var byBase))
        {
            return SectionHeader + byBase;
        }

        var regionalKey = ByLanguage.Keys.FirstOrDefault(
            key => key.StartsWith(baseLanguage + "-", StringComparison.OrdinalIgnoreCase));
        if (regionalKey is not null)
        {
            return SectionHeader + ByLanguage[regionalKey];
        }

        return SectionHeader + ByLanguage[FallbackLanguage];
    }
}
