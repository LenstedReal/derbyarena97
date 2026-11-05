using UnityEngine;

namespace DerbyArena.Core
{
    /// <summary>
    /// Two-language UI (Turkish / English). Every visible string is written next to its use as
    /// Loc.T("türkçe", "english"), so nothing can go missing from a separate table. The choice is saved;
    /// on first launch the phone language preselects it and the menu shows a language picker.
    /// </summary>
    public static class Loc
    {
        public const int Turkish = 0;
        public const int English = 1;

        public static bool IsEnglish { get { return Save.Data.Language == English; } }

        public static string T(string turkish, string english)
        {
            return Save.Data.Language == English ? english : turkish;
        }

        /// <summary>Before the player has chosen: follow the device language (Turkish phones -> Turkish).</summary>
        public static void AutoDetect()
        {
            if (Save.Data.LanguageChosen) return;
            Save.Data.Language = Application.systemLanguage == SystemLanguage.Turkish ? Turkish : English;
        }

        public static void Set(int language)
        {
            Save.Data.Language = language == English ? English : Turkish;
            Save.Data.LanguageChosen = true;
            Save.Write();
        }

        public static string LanguageName { get { return IsEnglish ? "ENGLISH" : "TÜRKÇE"; } }

        /// <summary>Capitals in the current language (4.0.2): in Turkish 'i' becomes 'İ' and 'ı' becomes 'I'
        /// (ToUpperInvariant turned "Demir Boğa" into "DEMIR BOĞA").</summary>
        public static string Upper(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (IsEnglish) return s.ToUpperInvariant();
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char ch in s) sb.Append(ch == 'i' ? 'İ' : ch == 'ı' ? 'I' : char.ToUpperInvariant(ch));
            return sb.ToString();
        }
    }
}
