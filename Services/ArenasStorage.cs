using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ArenaFights.Services
{
    /// <summary>
    /// Zapisywanie i wczytywanie listy Aren do pliku JSON: data/arenas.json
    /// Ścieżki graficzne trzymamy jako RELATYWNE (np. "assets/arenas/xxx.png", "assets/arenas/thumbs/xxx.png").
    /// </summary>
    public static class ArenasStorage
    {
        private const string DataFolderRel = "data";
        private const string ArenasJsonRel = DataFolderRel + "/arenas.json";

        /// <summary>DTO do serializacji (bez zależności na typy WPF).</summary>
        private sealed class ArenaDto
        {
            public string Nazwa { get; set; } = "";
            public string ObrazPath { get; set; } = "";             // relatywna
            public string MiniaturaPath { get; set; } = "";          // relatywna
            public List<SkillRefDto> Cechy { get; set; } = new();
        }

        private sealed class SkillRefDto
        {
            public string Nazwa { get; set; } = "";
            public string IkonaPath { get; set; } = "";              // relatywna
        }

        private static string Abs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }
        private static string AbsDir(string relDir) => Abs(relDir);

        /// <summary>Zapis listy Aren do JSON (data/arenas.json).</summary>
        public static void Save(IEnumerable<Models.Arena> arenas)
        {
            Directory.CreateDirectory(AbsDir(DataFolderRel));

            var list = (arenas ?? Enumerable.Empty<Models.Arena>()).Select(a => new ArenaDto
            {
                Nazwa = a.Nazwa ?? "",
                ObrazPath = ToRelative(a.ObrazPathAbsolute, a.ObrazPath),
                MiniaturaPath = ToRelative(a.MiniaturaPathAbsolute, null),
                Cechy = (a.Cechy ?? new List<Models.SkillRef>()).Select(c => new SkillRefDto
                {
                    Nazwa = c.Nazwa ?? "",
                    IkonaPath = ToRelative(c.IkonaPathAbsolute, null)
                }).ToList()
            }).ToList();

            var json = JsonConvert.SerializeObject(list, Formatting.Indented);
            File.WriteAllText(Abs(ArenasJsonRel), json);
        }

        /// <summary>Wczytanie listy Aren z JSON (jeśli plik istnieje). Zwraca relatywne -> absolutne ścieżki.</summary>
        public static List<Models.Arena> Load()
        {
            try
            {
                var abs = Abs(ArenasJsonRel);
                if (!File.Exists(abs)) return new List<Models.Arena>();

                var json = File.ReadAllText(abs);
                var list = JsonConvert.DeserializeObject<List<ArenaDto>>(json) ?? new List<ArenaDto>();

                return list.Select(d => new Models.Arena
                {
                    Nazwa = d.Nazwa ?? "",
                    ObrazPath = d.ObrazPath ?? "",
                    ObrazPathAbsolute = ToAbsolute(d.ObrazPath),
                    MiniaturaPathAbsolute = ToAbsolute(d.MiniaturaPath),
                    Cechy = (d.Cechy ?? new List<SkillRefDto>()).Select(c => new Models.SkillRef
                    {
                        Nazwa = c.Nazwa ?? "",
                        IkonaPathAbsolute = ToAbsolute(c.IkonaPath)
                    }).ToList()
                }).ToList();
            }
            catch
            {
                return new List<Models.Arena>();
            }
        }

        // Pomocnicze — konwersja rel/abs
        private static string ToAbsolute(string relOrEmpty)
        {
            if (string.IsNullOrWhiteSpace(relOrEmpty)) return "";
            return Abs(relOrEmpty);
        }

        /// <summary>
        /// Preferuj ścieżkę relatywną, jeżeli jest podana; w innym wypadku spróbuj zrobić relatywną z absolutnej.
        /// </summary>
        private static string ToRelative(string absOrEmpty, string alreadyRelative)
        {
            if (!string.IsNullOrWhiteSpace(alreadyRelative))
                return NormalizeRel(alreadyRelative);

            if (string.IsNullOrWhiteSpace(absOrEmpty))
                return "";

            // Spróbuj znormalizować do ścieżki względem baseDir
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var full = Path.GetFullPath(absOrEmpty);
            if (full.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                var rel = full.Substring(baseDir.Length).TrimStart('\\', '/')
                              .Replace('\\', '/');
                return rel;
            }
            // inny dysk/lokalizacja – zapisz absolutną, ale zamienioną na "pseudo-relatywną"
            return full.Replace('\\', '/');
        }

        private static string NormalizeRel(string rel)
        {
            return (rel ?? "").Trim().Replace('\\', '/');
        }
    }
}
