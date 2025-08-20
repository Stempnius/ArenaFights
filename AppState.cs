using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ArenaFights
{
    /// <summary>
    /// Wspólny stan aplikacji (umiejętności + areny). Rozszerzymy o zawodników przy rozdzielaniu kolejnych kart.
    /// </summary>
    public sealed class AppState
    {
        // ===== Umiejętności =====
        public ObservableCollection<SkillEntry> Skills { get; } = new();

        public void ReloadSkills()
        {
            Skills.Clear();
            var list = SkillsCatalogProvider.Load();
            foreach (var s in list) Skills.Add(s);
        }

        public void SaveSkills()
        {
            SkillsCatalogProvider.Save(Skills);
        }

        // ===== Areny =====
        public ObservableCollection<Models.Arena> Arenas { get; } = new();

        private const string DataFolderRel = "data";
        private const string ArenasJsonRel = DataFolderRel + "/arenas.json";

        public void ReloadArenas()
        {
            Arenas.Clear();

            var abs = Abs(ArenasJsonRel);
            if (!File.Exists(abs)) return;

            List<Models.Arena> list;
            try
            {
                var json = File.ReadAllText(abs);
                list = JsonConvert.DeserializeObject<List<Models.Arena>>(json) ?? new List<Models.Arena>();
            }
            catch
            {
                list = new List<Models.Arena>();
            }

            // Uzupełnij ścieżki absolutne, jeśli puste
            foreach (var a in list)
            {
                if (!string.IsNullOrWhiteSpace(a.ObrazPath))
                {
                    if (string.IsNullOrWhiteSpace(a.ObrazPathAbsolute))
                        a.ObrazPathAbsolute = ToAbs(a.ObrazPath);
                }

                if (string.IsNullOrWhiteSpace(a.MiniaturaPathAbsolute))
                    a.MiniaturaPathAbsolute = !string.IsNullOrWhiteSpace(a.ObrazPathAbsolute)
                        ? a.ObrazPathAbsolute
                        : (string.IsNullOrWhiteSpace(a.ObrazPath) ? "" : ToAbs(a.ObrazPath));

                // Cechy: jeśli IkonaPathAbsolute nieuzupełnione, spróbuj wyliczyć z IkonaPath (jeśli istnieje)
                if (a.Cechy != null)
                {
                    foreach (var c in a.Cechy)
                    {
                        if (!string.IsNullOrWhiteSpace(c.IkonaPathAbsolute)) continue;
                        // Jeśli w przyszłości SkillRef dostanie IkonaPath (względną), tutaj można ją przeliczyć.
                        // Teraz zakładamy, że IkonaPathAbsolute już jest (tak jak w Twoich danych).
                    }
                }

                Arenas.Add(a);
            }
        }

        public void SaveArenas()
        {
            Directory.CreateDirectory(AbsDir(DataFolderRel));
            var list = Arenas.ToList();
            var json = JsonConvert.SerializeObject(list, Formatting.Indented);
            File.WriteAllText(Abs(ArenasJsonRel), json);
        }

        // ===== Helpers =====
        private static string Abs(string rel)
        {
            var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }
        private static string AbsDir(string relDir) => Abs(relDir);

        private static string ToAbs(string rel)
        {
            var baseDir = System.AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
