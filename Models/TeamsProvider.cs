using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ArenaFights.Services
{
    public static class TeamsProvider
    {
        private const string DataFolderRel = "data";
        private const string TeamsJsonRel = DataFolderRel + "/teams.json";

        public static List<ArenaFights.Models.Team> Load()
        {
            try
            {
                var abs = Abs(TeamsJsonRel);
                if (!File.Exists(abs)) return new List<ArenaFights.Models.Team>();
                var json = File.ReadAllText(abs);
                var list = JsonConvert.DeserializeObject<List<ArenaFights.Models.Team>>(json)
                           ?? new List<ArenaFights.Models.Team>();
                return list
                    .GroupBy(t => t.Nazwa, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .OrderBy(t => t.Nazwa, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch { return new List<ArenaFights.Models.Team>(); }
        }

        public static void Save(IEnumerable<ArenaFights.Models.Team> teams)
        {
            Directory.CreateDirectory(AbsDir(DataFolderRel));
            var list = teams == null ? new List<ArenaFights.Models.Team>()
                                     : new List<ArenaFights.Models.Team>(teams);
            var json = JsonConvert.SerializeObject(list, Formatting.Indented);
            File.WriteAllText(Abs(TeamsJsonRel), json);
        }

        private static string Abs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }
        private static string AbsDir(string relDir) => Abs(relDir);
    }
}
