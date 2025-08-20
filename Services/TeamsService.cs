using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ArenaFights.Services
{
    public static class TeamsService
    {
        private const string DataFolderRel = "data";
        private const string TeamsJsonRel = DataFolderRel + "/teams.json";

        public static List<Models.Team> Load()
        {
            try
            {
                var path = Abs(TeamsJsonRel);
                if (!File.Exists(path)) return new List<Models.Team>();
                var json = File.ReadAllText(path);
                var list = JsonConvert.DeserializeObject<List<Models.Team>>(json) ?? new List<Models.Team>();
                // unikalne nazwy, posortowane
                return list
                    .Where(t => !string.IsNullOrWhiteSpace(t.Nazwa))
                    .GroupBy(t => t.Nazwa, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new Models.Team { Nazwa = g.Key })
                    .OrderBy(t => t.Nazwa, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            catch
            {
                return new List<Models.Team>();
            }
        }

        public static void Save(IEnumerable<Models.Team> teams)
        {
            Directory.CreateDirectory(AbsDir(DataFolderRel));
            var list = teams?.Where(t => !string.IsNullOrWhiteSpace(t.Nazwa))
                             .GroupBy(t => t.Nazwa, StringComparer.OrdinalIgnoreCase)
                             .Select(g => new Models.Team { Nazwa = g.Key })
                             .OrderBy(t => t.Nazwa, StringComparer.OrdinalIgnoreCase)
                             .ToList()
                       ?? new List<Models.Team>();

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
