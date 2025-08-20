using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using ArenaFights.Models;
using ArenaFights.Services;

namespace ArenaFights.Services
{
    /// <summary>
    /// Prosty zapis/odczyt aren do pliku JSON: data/arenas.json.
    /// W pliku przechowujemy ścieżki WZGLĘDNE (do assets), a przy ładowaniu
    /// upewniamy się, że miniatury są wygenerowane (AssetsService).
    /// </summary>
    public static class ArenasRepository
    {
        private const string DataFolderRel = "data";
        private const string ArenasJsonRel = DataFolderRel + "/arenas.json";

        // Model zapisu (DTO) – trzymamy tylko to, co potrzebne
        private sealed class ArenaDto
        {
            public string Nazwa { get; set; }
            public string ObrazPath { get; set; } // względny do assets/arenas/originals
            public List<SkillDto> Cechy { get; set; } = new();
        }

        private sealed class SkillDto
        {
            public string Nazwa { get; set; }
            public string IkonaPath { get; set; } // względny do assets/skills
        }

        public static void Save(IEnumerable<Arena> arenas)
        {
            try
            {
                Directory.CreateDirectory(AbsDir(DataFolderRel));
                var list = (arenas ?? Enumerable.Empty<Arena>())
                    .Select(a => new ArenaDto
                    {
                        Nazwa = a.Nazwa ?? "",
                        // Zapisujemy WZGLĘDNIE – a.ObrazPath powinno już być względne po imporcie
                        ObrazPath = MakeRelative(a.ObrazPath),
                        Cechy = (a.Cechy ?? new List<SkillRef>())
                            .Select(c => new SkillDto
                            {
                                Nazwa = c.Nazwa ?? "",
                                IkonaPath = MakeRelative(c.IkonaPathAbsolute) ?? MakeRelative(c.IkonaPathAbsolute), // i tak zapisujemy IkonaPath (rel)
                            })
                            .ToList()
                    })
                    .ToList();

                // W SkillDto.IkonaPath chcemy mieć REL do assets/skills; jeśli ktoś miał Absolute, zrzućmy do REL
                foreach (var ad in list)
                {
                    foreach (var s in ad.Cechy)
                    {
                        // Jeżeli IkonaPath nie wygląda na REL do assets/skills, zostaw puste (ikona zawsze może być później dobrana)
                        if (string.IsNullOrWhiteSpace(s.IkonaPath) ||
                            !s.IkonaPath.Replace('\\', '/').StartsWith(AssetsService.SkillsFolderRel, StringComparison.OrdinalIgnoreCase))
                        {
                            // nic – pozwalamy zapisać bez ikony albo w przyszłości poprawić
                        }
                    }
                }

                var json = JsonConvert.SerializeObject(list, Formatting.Indented);
                File.WriteAllText(Abs(ArenasJsonRel), json);
            }
            catch
            {
                // celowo cicho – Save nie może rozwalać aplikacji; błędy łapiemy w UI
            }
        }

        public static List<Arena> Load()
        {
            try
            {
                var abs = Abs(ArenasJsonRel);
                if (!File.Exists(abs)) return new List<Arena>();

                var json = File.ReadAllText(abs);
                var list = JsonConvert.DeserializeObject<List<ArenaDto>>(json) ?? new List<ArenaDto>();

                var result = new List<Arena>();

                foreach (var dto in list)
                {
                    var arena = new Arena
                    {
                        Nazwa = dto.Nazwa ?? "",
                        ObrazPath = dto.ObrazPath ?? ""
                    };

                    // Po stronie assets – upewnij się, że mamy miniaturę / ścieżki absolutne
                    if (!string.IsNullOrWhiteSpace(arena.ObrazPath))
                    {
                        // Zrób miniaturę jeśli brak, uzupełnij absolutne
                        try
                        {
                            // ImportArenaImage oczekuje ABS; zamień REL -> ABS
                            var sourceAbs = Abs(arena.ObrazPath);
                            if (File.Exists(sourceAbs))
                            {
                                // Wygeneruj świeżą miniaturę (lub nadpisz)
                                AssetsService.ImportArenaImage(sourceAbs, out var relOriginal, out var relThumb);
                                arena.ObrazPath = relOriginal;
                                arena.ObrazPathAbsolute = Abs(relOriginal);
                                arena.MiniaturaPathAbsolute = Abs(relThumb);
                            }
                            else
                            {
                                // fallback – jeśli obraz nie istnieje, arena nadal się załaduje, ale bez miniatury
                                arena.ObrazPathAbsolute = sourceAbs;
                                arena.MiniaturaPathAbsolute = sourceAbs;
                            }
                        }
                        catch
                        {
                            // nawet jeśli miniatura się nie zrobi – pokażemy co się da
                            var abs2 = Abs(arena.ObrazPath);
                            arena.ObrazPathAbsolute = abs2;
                            arena.MiniaturaPathAbsolute = abs2;
                        }
                    }

                    // Cechy
                    arena.Cechy = (dto.Cechy ?? new List<SkillDto>())
                        .Select(s => new SkillRef
                        {
                            Nazwa = s.Nazwa ?? "",
                            IkonaPathAbsolute = MakeAbsIfRel(s.IkonaPath)
                        })
                        .ToList();

                    result.Add(arena);
                }

                return result;
            }
            catch
            {
                return new List<Arena>();
            }
        }

        // ==== helpers ====

        private static string MakeRelative(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var p = path.Replace('\\', '/');

            // Jeśli już relatywny do assets – zostaw
            if (p.StartsWith("assets/", StringComparison.OrdinalIgnoreCase)) return p;

            // Jeśli absolutny – spróbuj zrzucić do rel od baseDir
            if (Path.IsPathRooted(p))
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory.Replace('\\', '/');
                if (!baseDir.EndsWith("/")) baseDir += "/";
                if (p.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
                {
                    var rel = p.Substring(baseDir.Length);
                    return rel;
                }
            }

            return p; // jak się nie da – zapisz jak jest (też zadziała)
        }

        private static string MakeAbsIfRel(string maybeRel)
        {
            if (string.IsNullOrWhiteSpace(maybeRel)) return null;
            if (Path.IsPathRooted(maybeRel)) return maybeRel;
            return Abs(maybeRel);
        }

        private static string Abs(string rel)
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, rel.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string AbsDir(string relDir) => Abs(relDir);
    }
}
