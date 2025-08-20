using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ArenaFights
{
    /// <summary>Katalog umiejętności trzymany w data/skills.json</summary>
    public static class SkillsCatalogProvider
    {
        private const string DataFolderRel = "data";
        private const string SkillsJsonRel = DataFolderRel + "/skills.json";

        public static List<SkillEntry> Load()
        {
            try
            {
                var abs = Abs(SkillsJsonRel);
                if (!File.Exists(abs)) return new List<SkillEntry>();
                var json = File.ReadAllText(abs);
                var list = JsonConvert.DeserializeObject<List<SkillEntry>>(json) ?? new List<SkillEntry>();
                return list
                    .GroupBy(s => s.Nazwa, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .OrderBy(s => s.Nazwa, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch { return new List<SkillEntry>(); }
        }

        public static void Save(IEnumerable<SkillEntry> skills)
        {
            Directory.CreateDirectory(AbsDir(DataFolderRel));
            var list = skills == null ? new List<SkillEntry>() : new List<SkillEntry>(skills);
            var json = JsonConvert.SerializeObject(list, Formatting.Indented);
            File.WriteAllText(Abs(SkillsJsonRel), json);
        }

        private static string Abs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }
        private static string AbsDir(string relDir) => Abs(relDir);
    }
}
