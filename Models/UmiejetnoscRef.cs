using System;
using System.IO;

namespace ArenaFights.Models
{
    /// <summary>Referencja do umiejętności przy zawodniku (nazwa + ścieżka do ikony).</summary>
    public class UmiejetnoscRef
    {
        public string Nazwa { get; set; } = "";
        /// <summary>Względna ścieżka (np. "assets/skills/xxx.png").</summary>
        public string IkonaPath { get; set; }

        public string IkonaPathAbsolute
        {
            get
            {
                if (string.IsNullOrWhiteSpace(IkonaPath)) return null;
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                return Path.Combine(baseDir, IkonaPath.Replace('/', Path.DirectorySeparatorChar));
            }
        }
    }
}
