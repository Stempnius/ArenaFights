using System;
using System.Collections.Generic;
using System.IO;

namespace ArenaFights.Models
{
    /// <summary>
    /// Arena turniejowa (obraz może być PNG/JPG/GIF; na liście używamy miniatury).
    /// </summary>
    public class Arena
    {
        /// <summary>Nazwa areny (unikalna).</summary>
        public string Nazwa { get; set; } = string.Empty;

        /// <summary>Ścieżka oryginalnego obrazu (REL lub ABS – zapisujemy zwykle REL).</summary>
        public string ObrazPath { get; set; } = string.Empty;

        /// <summary>Absolutna ścieżka do obrazu – jeżeli pusta, liczona z ObrazPath.</summary>
        public string ObrazPathAbsolute
        {
            get => _obrazPathAbsolute ?? ToAbsIfRel(ObrazPath);
            set => _obrazPathAbsolute = value; // „poduszka” – żeby nie psuć istniejących przypisań
        }
        private string _obrazPathAbsolute;

        /// <summary>Absolutna ścieżka do miniatury 250 px (lista Aren). Jeśli pusta, liczona z ObrazPath.</summary>
        public string MiniaturaPathAbsolute
        {
            get => _miniaturaPathAbsolute ?? ToAbsIfRel(ObrazPath);
            set => _miniaturaPathAbsolute = value;
        }
        private string _miniaturaPathAbsolute;

        /// <summary>Lista „cech” areny – korzystamy z tych samych ikon co umiejętności.</summary>
        public List<SkillRef> Cechy { get; set; } = new();

        // === Helpers ===
        internal static string ToAbsIfRel(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            // Jeśli wygląda na absolutną – zwróć bez zmian
            if (Path.IsPathRooted(path)) return path;
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, path.Replace('/', Path.DirectorySeparatorChar));
        }
    }

    /// <summary>
    /// Referencja do umiejętności/cechy (nazwa + ścieżki). 
    /// Działa z REL i ABS – jeżeli ABS nie podane, liczymy z REL.
    /// </summary>
    public class SkillRef
    {
        public string Nazwa { get; set; } = string.Empty;

        /// <summary>Ścieżka względna do ikony, np. "assets/skills/abc.png".</summary>
        public string IkonaPath { get; set; }

        /// <summary>
        /// Ścieżka absolutna do ikony. Jeśli nie ustawiona, jest wyliczana na podstawie IkonaPath.
        /// Zostawiamy setter, żeby nie wywracać istniejących przypisań.
        /// </summary>
        public string IkonaPathAbsolute
        {
            get => _ikonaPathAbsolute ?? Arena.ToAbsIfRel(IkonaPath);
            set => _ikonaPathAbsolute = value;
        }
        private string _ikonaPathAbsolute;
    }
}
