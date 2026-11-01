using UnityEngine;
using UnityEngine.SceneManagement;

namespace DerbyArena.Core
{
    /// <summary>
    /// Scene-level flow. Every match and the menu run in the same scene; switching reloads the scene,
    /// so no state (pools, AI, timers, physics) can leak between matches.
    /// </summary>
    public static class GameFlow
    {
        public static int PendingLevel;           // 0 = main menu
        public static string MenuPage = "home";   // page to open when returning to the menu

        public static void Play(int level)
        {
            Save.Load();
            int n = Mathf.Clamp(level, 1, Mathf.Min(Save.Data.Unlocked, Campaign.Total));
            Save.Data.LastPlayed = n;
            Save.Write();
            PendingLevel = n;
            Reload();
        }

        public static void ToMenu(string page)
        {
            PendingLevel = 0;
            MenuPage = string.IsNullOrEmpty(page) ? "home" : page;
            Reload();
        }

        static void Reload()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
