// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Health, symptom, diagnosis and medical terms a clarification question must never contain, per shipped
/// language (the four core languages plus every language pack; lower case, often word stems). Klacksy only
/// asks whether and when an employee will be absent, never about their health. Generic words for "sick"
/// (krank, sick, malade, malato, enfermo, ziek, sjuk, chory, nemocný, sairas, مريض, חולה, sakit, 病気, 아프,
/// ป่วย, 生病 and their equivalents) are deliberately NOT listed: "are you sick and cannot work your shift
/// today?" is exactly the question the dialog is meant to ask. The guard checks a question against all
/// languages at once, so every term must also be harmless inside the other languages' everyday words.
/// Three match modes: SubstringMatchLanguages match anywhere in the text, for scripts without spaces
/// between words (Japanese, Thai, Chinese), Korean (nouns take attached particles and form compounds such
/// as 허리통증) and Arabic/Hebrew (articles and prepositions are glued to the front of a word, as in
/// بالمستشفى or בבית חולים); CompoundSubstringMatchLanguages (German, Dutch, Danish, Norwegian, Swedish,
/// Finnish) glue nouns into closed compounds (Rückenschmerzen, hoofdpijn, huvudvärk, päänsärky) and also
/// match anywhere; all other languages match only at the start of a word. Terms of the substring modes
/// avoid short stems that occur inside other words (for example no bare Dutch "arts" because of "starts",
/// no bare Norwegian "lege" because of "kollege" and "verlegen"). AllowedAbsencePhrases are fixed
/// sick-leave phrasings that contain a listed term but only state an absence (arrêt maladie, in malattia,
/// ziekteverlof, sairausloma, إجازة مرضية, חופשת מחלה, ...); they are removed from the text before the terms
/// are matched and are always full phrases, never stems, so they cannot mask a real health term. The
/// non-core terms are LLM translations pending native-speaker review.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ClarificationHealthTerms
{
    public static readonly IReadOnlySet<string> SubstringMatchLanguages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ar", "he", "ja", "ko", "th", "zh-CN", "zh-TW" };

    public static readonly IReadOnlySet<string> CompoundSubstringMatchLanguages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "da", "de", "fi", "nb", "nl", "sv" };

    public static readonly IReadOnlyList<string> AllowedAbsencePhrases =
    [
        "arrêt maladie", "baixa por doença", "baja por enfermedad", "concediu de boala",
        "concediu de boală", "concediu medical", "congé maladie", "en maladie", "in malattia",
        "krankheitsbedingt", "licença por doença", "sairausloma", "sairauspoissaolo", "sick leave",
        "ziekteverlof", "ziekteverzuim", "ziektewet", "חופשת מחלה", "יום מחלה", "ימי מחלה",
        "إجازة مرضية", "اجازة مرضية", "الإجازة المرضية", "الاجازة المرضية"
    ];

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ByLanguage =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["de"] =
            [
                "symptom", "diagnos", "befund", "fieber", "schmerz", "arzt", "ärzt", "grippe", "corona",
                "covid", "erkält", "husten", "migräne", "kopfweh", "kopfschmerz", "übelkeit", "erbrech",
                "durchfall", "infekt", "entzündung", "verletz", "unfall", "operation", "schwanger", "psych",
                "depression", "depressiv", "burnout", "medikament", "krankheit", "beschwerde", "attest",
                "spital", "krankenhaus", "klinik", "therapie"
            ],
            ["en"] =
            [
                "symptom", "diagnos", "fever", "pain", "doctor", "physician", "influenza", "covid",
                "corona", "cough", "migraine", "headache", "nausea", "vomit", "diarrh", "infection",
                "inflammation", "injur", "accident", "surgery", "operation", "pregnan", "psych",
                "depressed", "depression", "depressive", "burnout", "medication", "medicine", "illness",
                "disease", "hospital", "clinic", "therapy", "medical"
            ],
            ["fr"] =
            [
                "symptôme", "diagnos", "fièvre", "douleur", "médecin", "docteur", "grippe", "covid",
                "corona", "toux", "migraine", "mal de tête", "nausée", "vomi", "diarrhée", "infection",
                "inflammation", "blessure", "blessé", "accident", "chirurgi", "opération", "enceinte",
                "grossesse", "psych", "dépress", "burnout", "médicament", "maladie", "hôpital", "clinique",
                "thérapie", "médical"
            ],
            ["it"] =
            [
                "sintom", "diagnos", "febbre", "dolor", "medic", "dottore", "influenza", "covid", "corona",
                "tosse", "emicrania", "mal di testa", "nausea", "vomit", "diarrea", "infezione",
                "infiammazione", "ferit", "incidente", "intervento", "operazione", "incinta", "gravidanza",
                "psic", "depressione", "depresso", "burnout", "farmac", "malattia", "ospedale", "clinica",
                "terapia"
            ],
            ["ar"] =
            [
                "أعراض", "تشخيص", "حمى", "حمّى", "سخونة", "ألم", "آلام", "أوجاع", "الوجع", "طبيب", "دكتور",
                "إنفلونزا", "انفلونزا", "زكام", "سعال", "كحة", "صداع", "غثيان", "تقيؤ", "استفراغ", "إسهال",
                "اسهال", "عدوى", "التهاب", "إصابة عمل", "جرح", "لحادث", "بحادث", "حادث عمل", "حادث سير",
                "جراح", "حامل", "مستشفى", "عيادة", "دواء", "أدوية", "اكتئاب", "مرض"
            ],
            ["cs"] =
            [
                "příznak", "diagnó", "horečk", "bolest", "bolí", "lékař", "doktor", "chřipk", "kašel",
                "kašl", "migrén", "nevoln", "zvrac", "průjem", "průjm", "infekc", "zánět", "zraněn", "úraz",
                "nehod", "operace", "operací", "chirurg", "těhotn", "nemocnic", "klinik", "léky", "léků",
                "léčb", "léčen", "deprese", "depresi", "onemocněn"
            ],
            ["da"] =
            [
                "symptom", "diagnos", "feber", "smerte", "ondt i", "læge", "influenza", "forkøl", "hoster",
                "migræne", "hovedpine", "kvalme", "opkast", "kaste op", "diarré", "infektion", "betændelse",
                "skadet", "arbejdsskad", "ulykke", "operer", "kirurg", "gravid", "hospital", "sygehus",
                "klinik", "medicin", "depression", "psyk", "sygdom"
            ],
            ["el"] =
            [
                "σύμπτωμ", "συμπτώμ", "διάγνωσ", "διαγνω", "πυρετ", "πόνο", "πονοκέφαλ", "πονά", "γιατρ",
                "γρίπη", "βήχα", "ημικρανία", "ναυτία", "εμετ", "έμετ", "διάρροι", "λοίμωξ", "μόλυνσ",
                "φλεγμον", "τραυματ", "ατύχημα", "χειρουργ", "επέμβασ", "έγκυος", "εγκυμοσ", "νοσοκομεί",
                "κλινική", "φάρμακ", "κατάθλιψ", "ψυχολογ", "ψυχίατρ", "ασθένει"
            ],
            ["es"] =
            [
                "síntoma", "diagnós", "fiebre", "dolor", "médico", "gripe", "tienes tos", "con tos",
                "toser", "tosiendo", "cefalea", "náusea", "vómito", "diarrea", "infección", "inflamación",
                "lesión", "lesionad", "herid", "accidente", "cirugía", "operación", "operaron", "operarte",
                "embaraz", "clínica", "medicament", "enfermedad", "depresi"
            ],
            ["fi"] =
            [
                "oirei", "oireet", "oireen", "diagnoo", "kuume", "kipua", "kipuja", "kivu", "särky", "säry",
                "lääkär", "lääke", "lääkit", "tohtori", "flunssa", "influenssa", "yskää", "yskän", "yskii",
                "migreeni", "pahoinvoin", "oksent", "ripuli", "tulehdu", "infektio", "tartunt", "vamma",
                "tapaturm", "onnettomuu", "kirurg", "operaatio", "raskaana", "raskaus", "sairaala",
                "terveyskesku", "masennu", "psyyk", "sairaus"
            ],
            ["he"] =
            [
                "תסמינ", "סימפטו", "אבחנ", "אבחון", "קדחת", "חום גבוה", "יש לך חום", "כאב", "כואב", "רופא",
                "שפעת", "שיעול", "מיגרנה", "בחילה", "הקאה", "הקאות", "הקיא", "שלשול", "זיהום", "דלקת",
                "פציעה", "פצוע", "תאונ", "ניתוח", "הריון", "היריון", "בית חולים", "קופת חולים", "מרפאה",
                "תרופ", "דיכאון", "פסיכ", "מחלה", "מחלת"
            ],
            ["id"] =
            [
                "gejala", "diagnosis", "demam", "nyeri", "sakit kepala", "sakit perut", "dokter",
                "kena flu", "terkena flu", "sedang flu", "influenza", "batuk", "pusing", "mual", "muntah",
                "diare", "infeksi", "radang", "cedera", "terluka", "kecelakaan", "operasi", "hamil",
                "kehamilan", "rumah sakit", "klinik", "obat", "depresi", "psikolog", "psikiat", "penyakit"
            ],
            ["ja"] =
            [
                "症状", "診断", "発熱", "熱が", "熱は", "痛み", "痛い", "頭痛", "腹痛", "腰痛", "医者", "医師", "病院", "受診", "診察",
                "通院", "入院", "インフルエンザ", "風邪", "咳が", "咳を", "咳は", "吐き気", "嘔吐", "下痢", "感染", "炎症", "怪我", "事故",
                "労災", "手術", "妊娠", "服薬", "薬を", "薬が", "うつ病", "精神", "疾患", "疾病"
            ],
            ["ko"] =
            [
                "증상", "진단", "열이 나", "열이 있", "열나", "발열", "고열", "통증", "두통", "복통", "요통", "의사", "병원", "진료",
                "독감", "감기", "기침", "메스꺼", "구토", "설사", "감염", "염증", "부상", "다쳤", "사고", "산재", "수술", "임신", "입원",
                "약을 먹", "약 먹", "약물", "복용", "우울", "정신과", "질병", "질환"
            ],
            ["ms"] =
            [
                "gejala", "diagnosis", "demam", "sakit kepala", "sakit perut", "doktor", "influenza",
                "selesema", "batuk", "pening kepala", "loya", "muntah", "cirit", "jangkitan", "radang",
                "kecederaan", "cedera", "kemalangan", "pembedahan", "hamil", "klinik", "ubat", "depresi",
                "kemurungan", "psikolog", "penyakit"
            ],
            ["nb"] =
            [
                "symptom", "diagnos", "feber", "smerte", "vondt i", "fastlege", "legevakt", "legekontor",
                "doktor", "influensa", "forkjøl", "hoster", "migrene", "hodepine", "kvalme", "oppkast",
                "kaste opp", "diaré", "infeksjon", "betennelse", "skadet", "arbeidsskad", "ulykke",
                "operer", "operasjon", "kirurg", "gravid", "sykehus", "klinikk", "medisin", "depresjon",
                "psyk", "sykdom"
            ],
            ["nl"] =
            [
                "symptom", "diagnos", "koorts", "pijn", "dokter", "de arts", "een arts", "huisarts",
                "tandarts", "bedrijfsarts", "griep", "hoest", "migraine", "misselijk", "overgeven",
                "diarree", "infectie", "ontsteking", "blessure", "gewond", "letsel", "ongeval", "operatie",
                "geopereerd", "zwanger", "ziekenhuis", "kliniek", "medicijn", "medicatie", "depressi",
                "psych", "ziekte"
            ],
            ["pl"] =
            [
                "objaw", "diagnoz", "gorączk", "ból głowy", "bólu", "bóle", "bólem", "boli", "lekarz",
                "doktor", "gryp", "kaszel", "kaszl", "migren", "mdłoś", "nudnoś", "wymiot", "biegunk",
                "infekcj", "zakażen", "zapaleni", "uraz", "kontuzj", "wypadek", "wypadk", "operacj",
                "zabieg", "ciąż", "szpital", "przychodni", "leki", "leków", "depresj", "choroba", "chorobę",
                "choroby", "chorobą"
            ],
            ["pt"] =
            [
                "sintoma", "diagnóst", "tem febre", "tens febre", "com febre", "febril", "dor de cabeça",
                "dores", "médico", "doutor", "gripe", "tosse", "enxaqueca", "náusea", "enjoo", "vômito",
                "vómito", "diarreia", "infecç", "infeç", "inflamaç", "lesão", "lesionad", "ferid",
                "acidente", "cirurgi", "grávida", "gravidez", "clínica", "medicament", "remédio", "doença",
                "depressã", "psicólog"
            ],
            ["ro"] =
            [
                "simptom", "diagnos", "febră", "febra", "febril", "durere", "dureri", "gripă", "gripa",
                "tuse", "migren", "greață", "greata", "vărsătur", "voma", "diaree", "infecț", "inflamaț",
                "rănit", "rană", "accident", "operație", "operatie", "chirurg", "gravidă", "gravida",
                "însărcinată", "insarcinata", "spital", "clinic", "medicament", "depresi", "psih", "boală",
                "boala"
            ],
            ["sv"] =
            [
                "symptom", "diagnos", "feber", "smärt", "värk", "läkare", "doktor", "vårdcentral",
                "influensa", "förkyl", "hostning", "hostar", "migrän", "illamå", "må illa", "mår illa",
                "mår du illa", "kräk", "diarré", "infektion", "inflammation", "skadad", "arbetsskad",
                "olycka", "olyckan", "opererad", "operera", "kirurg", "gravid", "sjukhus", "klinik",
                "medicin", "läkemedel", "psyk", "sjukdom"
            ],
            ["th"] =
            [
                "อาการ", "วินิจฉัย", "ไข้", "ปวด", "เจ็บปวด", "หมอ", "แพทย์", "เป็นหวัด", "ไอมาก", "ไอแห้ง",
                "ไอหนัก", "ไอไหม", "ไอบ่อย", "คลื่นไส้", "อาเจียน", "ท้องเสีย", "ติดเชื้อ", "อักเสบ",
                "บาดเจ็บ", "อุบัติเหตุ", "ผ่าตัด", "ตั้งครรภ์", "โรงพยาบาล", "คลินิก", "กินยา", "ทานยา",
                "ซึมเศร้า", "โรค"
            ],
            ["vi"] =
            [
                "triệu chứng", "chẩn đoán", "bị sốt", "sốt cao", "cơn sốt", "bị đau", "cơn đau", "đau đầu",
                "nhức đầu", "đau bụng", "đau lưng", "bác sĩ", "bị cúm", "cảm cúm", "cơn ho", "bị ho không",
                "ho nhiều", "ho khan", "buồn nôn", "nôn mửa", "bị nôn", "tiêu chảy", "nhiễm trùng", "viêm",
                "chấn thương", "bị thương", "tai nạn", "phẫu thuật", "ca mổ", "có thai", "mang thai",
                "bệnh viện", "nhập viện", "phòng khám", "thuốc", "trầm cảm", "tâm thần", "căn bệnh"
            ],
            ["zh-CN"] =
            [
                "症状", "诊断", "发烧", "发热", "疼痛", "头疼", "头痛", "肚子疼", "疼吗", "疼不疼", "很疼", "哪里疼", "痛吗", "腹痛", "医生",
                "大夫", "看病", "流感", "感冒", "咳嗽", "恶心", "呕吐", "腹泻", "拉肚子", "感染", "发炎", "受伤", "工伤", "事故", "手术",
                "怀孕", "医院", "住院", "吃药", "药物", "抑郁", "心理", "疾病"
            ],
            ["zh-TW"] =
            [
                "症狀", "診斷", "發燒", "發熱", "疼痛", "頭疼", "頭痛", "肚子痛", "疼嗎", "痛嗎", "很痛", "哪裡痛", "腹痛", "醫生", "醫師",
                "看病", "流感", "感冒", "咳嗽", "噁心", "嘔吐", "腹瀉", "拉肚子", "感染", "發炎", "受傷", "工傷", "事故", "手術", "懷孕",
                "醫院", "住院", "吃藥", "藥物", "憂鬱", "心理", "疾病"
            ]
        };
}
