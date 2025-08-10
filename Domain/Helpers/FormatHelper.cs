namespace Klacks.Api.Domain.Helpers
{
    public static class FormatHelper
    {
        public static string ReplaceNoisyWords(string value)
        {
            return value
              .Replace(" wie ", " ")
              .Replace(" ich ", " ")
              .Replace(" seine ", " ")
              .Replace(" dass ", " ")
              .Replace(" er ", " ")
              .Replace(" war ", " ")
              .Replace(" für ", " ")
              .Replace(" auf ", " ")
              .Replace(" sind ", " ")
              .Replace(" mit ", " ")
              .Replace(" sie ", " ")
              .Replace(" sein ", " ")
              .Replace(" bei ", " ")
              .Replace(" ein ", " ")
              .Replace(" dies ", " ")
              .Replace(" aus ", " ")
              .Replace(" aber ", " ")
              .Replace(" was ", " ")
              .Replace(" ist ", " ")
              .Replace(" es ", " ")
              .Replace(" die ", " ")
              .Replace(" von ", " ")
              .Replace(" zu ", " ")
              .Replace(" und ", " ")
              .Replace(" ein ", " ")
              .Replace(" bei ", " ")
              .Replace(" wir ", " ")
              .Replace(" aus ", " ")
              .Replace(" die ", " ")
              .Replace(" tun ", " ")
              .Replace(" ihre ", " ")
              .Replace(" wenn ", " ")
              .Replace(" wie ", " ")
              .Replace(" ein ", " ")
              .Replace(" auch ", " ")
              .Replace(" so ", " ")
              .Replace(" aus ", " ")
              .Replace(" wo ", " ")
              .Replace(" zu ", " ")
              .Replace(" dann ", " ");
        }

        public static string ReplaceUmlaud(string value)
        {
            value = value.ToLower();
            value = ReplaceA(value);
            value = ReplaceU(value);
            value = ReplaceE(value);
            value = ReplaceO(value);
            value = ReplaceI(value);
            value = ReplaceC(value);
            value = ReplaceS(value);
            value = ReplaceR(value);
            value = ReplaceZ(value);
            value = ReplaceOther(value);

            return value;
        }

        private static string ReplaceA(string value)
        {
            return value
              .Replace("á", "a")
              .Replace("à", "a")
              .Replace("â", "a")
              .Replace("ä", "a")
              .Replace("ă", "a")
              .Replace("ą", "a")
              .Replace("å", "a")
              .Replace("æ", "a")
              .Replace("ã", "a");
        }

        private static string ReplaceC(string value)
        {
            return value
              .Replace("ç", "c")
              .Replace("ć", "c")
              .Replace("ĉ", "c")
              .Replace("ċ", "c")
              .Replace("č", "c")
              .Replace("ċ", "c");
        }

        private static string ReplaceE(string value)
        {
            return value
              .Replace("é", "e")
              .Replace("è", "e")
              .Replace("ë", "e")
              .Replace("ê", "e")
              .Replace("ȩ", "e");
        }

        private static string ReplaceI(string value)
        {
            return value
              .Replace("ì", "i")
              .Replace("í", "i")
              .Replace("î", "i")
              .Replace("ï", "i");
        }

        private static string ReplaceO(string value)
        {
            return value
              .Replace("ò", "o")
              .Replace("ó", "o")
              .Replace("ô", "o")
              .Replace("õ", "o")
              .Replace("ö", "o")
              .Replace("ō", "o")
              .Replace("ŏ", "o")
              .Replace("œ", "o");
        }

        private static string ReplaceOther(string value)
        {
            return value
              .Replace("`", "")
              .Replace("'", "")
              .Replace("-", "")
              .Replace("/", "")
              .Replace(" ", "")
              .Replace("?", "")
              .Replace("!", "")
              .Replace("*", "")
              .Replace("#", "")
              .Replace("&", "")
              .Replace("£", "")
              .Replace("$", "")
              .Replace("~", "")
              .Replace("^", "")
              .Replace("´", "")
              .Replace("{", "")
              .Replace("}", "")
              .Replace("[", "")
              .Replace("]", "")
              .Replace("¨", "")
              .Replace("\\", "");
        }

        private static string ReplaceR(string value)
        {
            return value
              .Replace("ŕ", "r")
              .Replace("ŗ", "r")
              .Replace("ř", "r");
        }

        private static string ReplaceS(string value)
        {
            return value
              .Replace("ś", "s")
              .Replace("ŝ", "s")
              .Replace("ş", "s")
              .Replace("š", "s");
        }

        private static string ReplaceU(string value)
        {
            return value
              .Replace("ú", "u")
              .Replace("ù", "u")
              .Replace("û", "u")
              .Replace("ü", "u")
              .Replace("ů", "u")
              .Replace("ū", "u")
              .Replace("ŭ", "u")
              .Replace("ų", "u")
              .Replace("ů", "u");
        }

        private static string ReplaceZ(string value)
        {
            return value
              .Replace("ź", "z")
              .Replace("ż", "z")
              .Replace("ž", "z");
        }
    }
}
