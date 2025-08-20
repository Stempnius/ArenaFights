using System;
using System.IO;

namespace ArenaFights.Models
{
    /// <summary>
    /// Wpis w katalogu umiejętności (to, co widzisz w zakładce „Umiejętności”).
    /// Uwaga: dodajemy CzySlabosc i seter IkonaPathAbsolute, żeby nie wybuchały
    /// żadne starsze wiązania ani przypisania.
    /// </summary>
    public class Skill
    {
        /// <summary>Nazwa umiejętności (unikalna).</summary>
        public string Nazwa { get; set; } = "";

        /// <summary>Ścieżka względna do pliku ikony, np. "assets/skills/abc.png".</summary>
        public string IkonaPath { get; set; }

        /// <summary>
        /// Pełna ścieżka absolutna do pliku ikony – normalnie WYLICZANA z IkonaPath.
        /// Dajemy setter, żeby nie sypały się miejsca, które próbują przypisać (stary kod).
        /// </summary>
        public string IkonaPathAbsolute
        {
            get
            {
                if (string.IsNullOrWhiteSpace(IkonaPath)) return null;
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(baseDir, IkonaPath.Replace('/', Path.DirectorySeparatorChar));
            }
            set
            {
                // „Poduszka” – pozwalamy przypisać, ale nie używamy tej wartości nigdzie indziej.
                // Chodzi o to, by stare przypisania nie powodowały błędów kompilacji/uruchomienia.
            }
        }

        /// <summary>
        /// Flaga używana tylko przez starsze szablony/XAML.
        /// Katalog sam w sobie nie rozróżnia „słabości”; słabość to po prostu
        /// referencja do tej samej umiejętności w innym miejscu.
        /// </summary>
        public bool CzySlabosc { get; set; } = false;
    }
}
