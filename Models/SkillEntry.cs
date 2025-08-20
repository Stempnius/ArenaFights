using System;
using System.IO;

namespace ArenaFights
{
    public class SkillEntry
    {
        public string Nazwa { get; set; } = "";
        public string IkonaPath { get; set; }  // ścieżka WZGLĘDNA lub null

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
